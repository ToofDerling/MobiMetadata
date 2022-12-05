namespace MobiMetadata
{
    public class EXTHRecord
    {
        private readonly byte[] _recordTypeData = new byte[4];
        private readonly byte[] _recordLength = new byte[4];
        private readonly byte[] _recordData;

        public EXTHRecord(Stream stream, Dictionary<int, object> recordTypesToRead)
        {
            stream.Read(_recordTypeData, 0, _recordTypeData.Length);

            stream.Read(_recordLength, 0, _recordLength.Length);
            if (RecordLength < 8)
            {
                throw new MobiMetadataException("Invalid EXTH record length");
            }

            var dataLength = RecordLength - 8;

            if (recordTypesToRead == null || recordTypesToRead.ContainsKey((int)RecordType))
            {
                _recordData = new byte[dataLength];
                stream.Read(_recordData, 0, _recordData.Length);
            }
            else
            {
                stream.Position += dataLength;
            }
        }

        //Properties
        public int DataLength => _recordData.Length;

        public int Size => DataLength + 8;

        public uint RecordLength => Converter.ToUInt32(_recordLength);

        public uint RecordType => Converter.ToUInt32(_recordTypeData);

        public byte[] RecordData => _recordData;
    }
}
