namespace MobiMetadata
{
    public sealed class EXTHRecord : BaseRecord
    {
        private const int _recordTypePos = 0;
        private const int _recordTypeLen = 4;

        private const int _recordLengthPos = 4;
        private const int _recordLengthLen = 4;

        private const int _dataPos = 8;
        private readonly int _dataLen;

        public EXTHRecord(Memory<byte> recordsData, int recordPosition) : base(recordsData, recordPosition)
        {
            var recordLength = (int)RecordLength;
            if (recordLength < 8)
            {
                throw new MobiMetadataException("Invalid EXTH record length");
            }

            var dataLength = recordLength - (_recordTypeLen + _recordLengthLen);
            _dataLen = dataLength;
        }

        //Properties
        public int DataLength => _dataLen;

        public int Size => DataLength + 8;

        public uint RecordLength => Converter.ToUInt32(GetPropertyData(_recordLengthPos, _recordLengthLen).Span);

        public uint RecordType => Converter.ToUInt32(GetPropertyData(_recordTypePos, _recordTypeLen).Span);

        public Memory<byte> RecordData => GetPropertyData(_dataPos, _dataLen);
    }
}
