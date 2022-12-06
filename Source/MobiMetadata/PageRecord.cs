using System.Buffers;
using System.Text;

namespace MobiMetadata
{
    public class PageRecord
    {
        protected readonly Stream _stream;
        protected readonly long _pos;
        protected readonly int _len;

        protected readonly BinaryReader _reader;

        public string PageRef { get; set; }

        public PageRecord(Stream stream, long pos, uint len)
        {
            _stream = stream;
            _pos = pos;
            _len = (int)len;
            _reader = new BinaryReader(stream);
        }

        public async Task<RescRecord> GetRescRecordAsync()
        {
            _stream.Position = _pos;

            RescRecord rescRecord = null;
            if (!await IsRecordIdAsync("RESC"))
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

        public bool IsCresRecord { get; protected set; }

        public int Length => _len;

#if !DEBUG
        protected async Task<bool> IsRecordIdAsync(string id)
        {
            var idLen = id.Length;

            var bytes = ArrayPool<byte>.Shared.Rent(idLen);
            
            var memory = bytes.AsMemory(0, idLen);
            var read = await _stream.ReadAsync(memory);

            if (read != idLen)
            {
                throw new MobiMetadataException($"Error reading record. Expected {idLen} bytes, got {read}");
            }

            var sequence = new ReadOnlySequence<byte>(memory);
            var res = Encoding.ASCII.GetString(sequence) == id;

            ArrayPool<byte>.Shared.Return(bytes);

            return res;
        }
#else
        protected async Task<bool> IsRecordIdAsync(string id)
        {
            var peekLen = Math.Min(32, _len);

            var bytes = ArrayPool<byte>.Shared.Rent(peekLen);

            var memory = bytes.AsMemory(0, peekLen);
            var read = await _stream.ReadAsync(memory);

            if (read != peekLen)
            {
                throw new MobiMetadataException($"Error reading record. Expected {peekLen} bytes, got {read}");
            }

            var sequence = new ReadOnlySequence<byte>(memory);
            var idx = Encoding.ASCII.GetString(sequence).IndexOf(id);

            ArrayPool<byte>.Shared.Return(bytes);
            
            if (idx > 0)
            {
                throw new MobiMetadataException($"Got expected identifier {id} at unexpected postion {idx}");
            }
            return idx == 0;
        }
#endif

        public virtual Span<byte> ReadData()
        {
            _stream.Position = _pos;

            return _reader.ReadBytes(_len);
        }
    }
}
