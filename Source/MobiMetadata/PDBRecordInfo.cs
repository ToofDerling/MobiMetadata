namespace MobiMetadata
{
    public class PDBRecordInfo
    {
        private readonly byte[] _recordDataOffset = new byte[4];
        //private readonly byte _recordAttributes = 0;
        private readonly byte[] _uniqueID = new byte[3];

        public PDBRecordInfo(Stream stream, bool readRecordInfo)
        {
            if (readRecordInfo)
            {
                stream.Read(_recordDataOffset, 0, _recordDataOffset.Length);
                
                stream.Position++;
                //_recordAttributes = (byte)stream.ReadByte();
                
                stream.Read(_uniqueID, 0, _uniqueID.Length);
            }
            else
            {
                stream.Position += 8;
            }
        }

        public uint RecordDataOffset => Converter.ToUInt32(_recordDataOffset);
    }
}
