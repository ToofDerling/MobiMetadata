using System.Buffers;
using System.Text;

namespace MobiMetadata
{
    public class PageRecord
    {
        protected readonly Stream _stream;
        protected readonly long _pos;
        protected readonly int _len;

        public PageRecord(Stream stream, long pos, uint len)
        {
            _stream = stream;
            _pos = pos;
            _len = (int)len;
        }

        public async Task<RescRecord?> GetRescRecordAsync()
        {
            _stream.Position = _pos;

            RescRecord rescRecord = null;
            if (!await IsRecordIdAsync(RescRecord.RecordId))
            {
                return rescRecord;
            }

            rescRecord = new RescRecord(_stream, _pos, (uint)_len);
            return rescRecord;
        }

        public bool IsLen1992Record()
        {
            return _len == 1992;
        }

        public async Task<bool> IsDatpRecordAsync()
        {
            _stream.Position = _pos;

            return await IsRecordIdAsync("DATP");
        }

        public async Task<bool> IsKindleEmbedRecordAsync()
        {
            _stream.Position = _pos;

            return await IsRecordIdAsync("kindle:embed");
        }

        public async Task<bool> IsCresRecordAsync()
        {
            _stream.Position = _pos;

            return await IsRecordIdAsync(ImageRecordHD.RecordId);
        }

        public int Length => _len;

        private bool TestRecordId(string id, Memory<byte> memory)
        {
            var slice = memory[..id.Length];

            var res = Encoding.ASCII.GetString(slice.Span) == id;

            return res;
        }

#if !DEBUG
        protected async Task<bool> IsRecordIdAsync(string id)
        {
            var idLen = id.Length;

            var str = await GetDataAsStringAsync(length: idLen);

            return str == id;
        }
#else
        protected async Task<bool> IsRecordIdAsync(string id)
        {
            var peekLen = Math.Min(32, _len);

            var str = await GetDataAsStringAsync(length: peekLen);

            var idx = str.IndexOf(id);

            if (idx > 0)
            {
                throw new MobiMetadataException($"Got expected identifier {id} at unexpected postion {idx}");
            }

            return idx == 0;
        }
#endif

        public virtual async Task<string> GetDataAsStringAsync(int length = 0, int skip = 0)
        {
            var len = length > 0 ? length : _len;

            len -= skip;
            _stream.Position = _pos + skip;

            var bytes = ArrayPool<byte>.Shared.Rent(len);
            try
            {
                var memory = await ReadDataAsync(bytes, len);
                return Encoding.UTF8.GetString(memory.Span);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }

        protected virtual int GetMagic()
        {
            return 0;
        }

        private async Task<Memory<byte>> ReadDataAsync(byte[] bytes, int length)
        {
            var memory = bytes.AsMemory(0, length);
            var read = await _stream.ReadAsync(memory);

            if (read != length)
            {
                throw new MobiMetadataException($"Error reading record. Expected {length} bytes, got {read}");
            }

            return memory;
        }

        public virtual async Task<bool> WriteDataAsync(Stream toStream, string recordId = null, string file = null)
        {
            var bytes = ArrayPool<byte>.Shared.Rent(_len);
            try
            {
                _stream.Position = _pos;

                var memory = await ReadDataAsync(bytes, _len);

                if (recordId != null && !TestRecordId(recordId, memory))
                {
                    return false;
                }

                var magic = GetMagic();
                memory = magic > 0 ? memory[magic..] : memory;

                await toStream.WriteAsync(memory);

                if (file != null)
                {
                    using var fileStream
                        = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None, 0, FileOptions.Asynchronous | FileOptions.SequentialScan);

                    await fileStream.WriteAsync(memory);
                }

                return true;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }
    }
}
