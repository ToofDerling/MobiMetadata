namespace MobiMetadata
{
    public class PDBRecordInfo
    {
        public static int PdbRecordLen => 8;

        private readonly int _recordDataOffsetLen = 4;
        private readonly int _recordDataOffsetPos = 0;
        //private readonly byte _recordAttributes = 0;
        //private readonly byte[] _uniqueID = new byte[3];

        private readonly Memory<byte> _recordsData;

        private readonly int _recordPosition;

        public PDBRecordInfo(Memory<byte> recordsData, int recordPosition)
        {
            _recordsData = recordsData;
            _recordPosition = recordPosition;
        }

        private Memory<byte> GetPropertyData(int pos, int len)
        {
            return _recordsData.Slice(_recordPosition + pos, len);
        }

        public uint RecordDataOffset
            => Converter.ToUInt32(GetPropertyData(_recordDataOffsetPos, _recordDataOffsetLen).Span);
    }
}
