namespace MobiMetadata
{
    public sealed class PDBRecordInfo : BaseRecord
    {
        public static int PdbRecordLen => 8;

        private const int _recordDataOffsetPos = 0;
        private const int _recordDataOffsetLen = 4;

        //private readonly byte _recordAttributes = 0;
        //private readonly byte[] _uniqueID = new byte[3];

        public PDBRecordInfo(Memory<byte> recordsData, int recordPosition) : base(recordsData, recordPosition)
        {
        }

        public uint RecordDataOffset => GetPropertyDataAsUint(_recordDataOffsetPos, _recordDataOffsetLen);
    }
}
