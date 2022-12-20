using System.Buffers;
using System.Text;

namespace MobiMetadata
{
    public class PageRecord
    {
        protected readonly Stream _stream;
        protected readonly long _pos;
        protected readonly int _len;

        protected class RecordId
        {
            public static Memory<byte> CRES => Encoding.ASCII.GetBytes("CRES");

            public static Memory<byte> KindleEmbed => Encoding.ASCII.GetBytes("kindle:embed");

            public static Memory<byte> DATP => Encoding.ASCII.GetBytes("DATP");

            public static Memory<byte> RESC => Encoding.ASCII.GetBytes("RESC");
        }

        public PageRecord(Stream stream, long pos, uint len)
        {
            _stream = stream;
            _pos = pos;
            _len = (int)len;
        }

        public async Task<RescRecord?> GetRescRecordAsync()
        {
            _stream.Position = _pos;

            RescRecord rescRecord = null!;
            if (!await IsRecordIdAsync(RecordId.RESC).ConfigureAwait(false))
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

            return await IsRecordIdAsync(RecordId.DATP).ConfigureAwait(false);
        }

        public async Task<bool> IsKindleEmbedRecordAsync()
        {
            _stream.Position = _pos;

            return await IsRecordIdAsync(RecordId.KindleEmbed).ConfigureAwait(false);
        }

        public async Task<bool> IsCresRecordAsync()
        {
            _stream.Position = _pos;

            return await IsRecordIdAsync(RecordId.CRES).ConfigureAwait(false);
        }

        public int Length => _len;

        protected async Task<bool> IsRecordIdAsync(Memory<byte> recordId)
        {
            var idLen = recordId.Length;

            var bytes = ArrayPool<byte>.Shared.Rent(idLen);
            try
            {
                var data = await ReadDataAsync(bytes, idLen).ConfigureAwait(false);

                return data.Span.SequenceEqual(recordId.Span);
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

        protected async Task<Memory<byte>> ReadDataAsync(byte[] bytes, int length)
        {
            var memory = bytes.AsMemory(0, length);
            var read = await _stream.ReadAsync(memory).ConfigureAwait(false);

            if (read != length)
            {
                throw new MobiMetadataException($"Error reading record. Expected {length} bytes, got {read}");
            }

            return memory;
        }

        public async Task<bool> TryWriteHDImageDataAsync(params Stream[] streams)
        {
            return await WriteDataCoreAsync(RecordId.CRES, streams).ConfigureAwait(false);
        }

        public async Task WriteDataAsync(params Stream[] streams)
        {
            await WriteDataCoreAsync(null!, streams).ConfigureAwait(false);
        }

        private async Task<bool> WriteDataCoreAsync(Memory<byte>? recordId = null, params Stream[] streams)
        {
            var bytes = ArrayPool<byte>.Shared.Rent(_len);
            try
            {
                _stream.Position = _pos;

                var memory = await ReadDataAsync(bytes, _len).ConfigureAwait(false);

                // Only write data if this record begins with the specified recordId 
                if (recordId.HasValue && !memory.Span.StartsWith(recordId.Value.Span))
                {
                    return false;
                }

                var magic = GetMagic();
                memory = magic > 0 ? memory[magic..] : memory;

                foreach (var stream in streams)
                {
                    if (stream != null) 
                    {
                        await stream.WriteAsync(memory).ConfigureAwait(false);
                    }
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
