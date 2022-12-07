using System;

namespace MobiMetadata
{
    public class EXTHRecord
    {
        private readonly byte[] _recordTypeData = new byte[4];
        private readonly byte[] _recordLength = new byte[4];
        
        private byte[] _recordData;
        private readonly Dictionary<int, object> _recordTypesToRead;

        public EXTHRecord(Dictionary<int, object> recordTypesToRead)
        {
            _recordTypesToRead = recordTypesToRead;
        }

        public async Task ReadRecordAsync(Stream stream)
        {
            await stream.ReadAsync(_recordTypeData);

            await stream.ReadAsync(_recordLength);
            if (RecordLength < 8)
            {
                throw new MobiMetadataException("Invalid EXTH record length");
            }

            var dataLength = RecordLength - 8;

            if (_recordTypesToRead == null || _recordTypesToRead.ContainsKey((int)RecordType))
            {
                _recordData = new byte[dataLength];
                await stream.ReadAsync(_recordData);
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
