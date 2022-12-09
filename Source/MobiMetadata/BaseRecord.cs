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

        protected Memory<byte> GetPropertyData(int pos, int len)
        {
            return RecordsData.Slice(_recordPosition + pos, len);
        }
    }
}
