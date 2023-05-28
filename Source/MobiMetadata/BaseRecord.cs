namespace MobiMetadata
{
    public class BaseRecord
    {
        private readonly int _recordPosition;

        public BaseRecord(Memory<byte> recordsData, int recordPosition)
        {
            RecordsData = recordsData;
            _recordPosition = recordPosition;
        }

        private Memory<byte> RecordsData { get; set; }

        protected Memory<byte> GetPropertyData(int pos, int len) => RecordsData.Slice(_recordPosition + pos, len);

        protected uint GetPropertyDataAsUint(int pos, int len) => Converter.ToUInt32(GetPropertyData(pos, len).Span);
    }
}
