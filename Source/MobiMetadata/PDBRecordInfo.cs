namespace MobiMetadata
{
    public class PDBRecordInfo
    {
        private readonly byte[] _recordDataOffset = new byte[4];
        //private readonly byte _recordAttributes = 0;
        //private readonly byte[] _uniqueID = new byte[3];

        private readonly bool _readRecordInfo;

        public PDBRecordInfo(bool readRecordInfo)
        {
            _readRecordInfo = readRecordInfo;
        }

        public async Task ReadRecordInfoAsync(Stream stream)
        {
            if (_readRecordInfo)
            {
                await stream.ReadAsync(_recordDataOffset);

                //stream.Position++;
                //_recordAttributes = (byte)stream.ReadByte();

                //stream.Read(_uniqueID, 0, _uniqueID.Length);
                stream.Position += 4;
            }
            else
            {
                stream.Position += 8;
            }
        }

        public uint RecordDataOffset => Converter.ToUInt32(_recordDataOffset);
    }
}
