namespace MobiMetadata
{
    public class EXTHHead : BaseHead
    {
        private static readonly List<Attr> _exthHeadAttrs = new();

        private static readonly Attr _identifierAttr = new(4, _exthHeadAttrs);

        private static readonly Attr _headerLengthAttr = new(4, _exthHeadAttrs);

        private static readonly Attr _recordCountAttr = new(4, _exthHeadAttrs);

        private EXTHRecord[] _recordList;
        
        private Memory<byte> RecordsData { get; set; }

        internal override async Task ReadHeaderAsync(Stream stream)
        {
            var attrLen = _exthHeadAttrs.Sum(x => x.Length);
            await ReadHeaderDataAsync(stream, attrLen).ConfigureAwait(false);

            if (IdentifierAsString != "EXTH")
            {
                throw new MobiMetadataException("Did not get expected EXTH identifier");
            }

            RecordsData = new byte[HeaderLength - attrLen];
            await stream.ReadAsync(RecordsData).ConfigureAwait(false);

            var recordPos = 0;
            var recordCount = RecordCount;

            _recordList = new EXTHRecord[recordCount];

            for (int i = 0; i < recordCount; i++)
            {
                var exthRecord = new EXTHRecord(RecordsData, recordPos);

                _recordList[i] = exthRecord;

                recordPos += exthRecord.Size;
            }
        }

        public int Size
        {
            get
            {
                var dataSize = _recordList.Sum(r => r.Size);
                return 12 + dataSize + GetPaddingSize(dataSize);
            }
        }

        private static int GetPaddingSize(int dataSize)
        {
            var paddingSize = dataSize % 4;

            if (paddingSize != 0)
            {
                paddingSize = 4 - paddingSize;
            }

            return paddingSize;
        }

        //Properties
        public string IdentifierAsString => GetPropAsUtf8RemoveNull(_identifierAttr);

        public uint HeaderLength => GetPropAsUint(_headerLengthAttr);

        public uint RecordCount => GetPropAsUint(_recordCountAttr);

        //Records
        public string Author => GetRecordAsString(100);

        public string Publisher => GetRecordAsString(101);

        public string Imprint => GetRecordAsString(102);

        public string Description => GetRecordAsString(103);

        public string IBSN => GetRecordAsString(104);

        public string Subject => GetRecordAsString(105);

        public string PublishedDate => GetRecordAsString(106);

        public string Review => GetRecordAsString(107);

        public string Contributor => GetRecordAsString(108);

        public string Rights => GetRecordAsString(109);

        public string SubjectCode => GetRecordAsString(110);

        public string Type => GetRecordAsString(111);

        public string Source => GetRecordAsString(112);

        public string ASIN => GetRecordAsString(113);

        public string VersionNumber => GetRecordAsString(114);

        public string RetailPrice => GetRecordAsString(118);

        public string RetailPriceCurrency => GetRecordAsString(119);

        public uint Kf8BoundaryOffset => GetRecordAsUint(121);

        public string BookType => GetRecordAsString(123);

        public uint RescOffset => GetRecordAsUint(131);

        public string DictionaryShortName => GetRecordAsString(200);

        public uint CoverOffset => GetRecordAsUint(201);

        public uint ThumbOffset => GetRecordAsUint(202);

        public uint HasFakeCover => GetRecordAsUint(203);

        public string CDEType => GetRecordAsString(501);

        public string UpdatedTitle => GetRecordAsString(503);

        public string ASIN2 => GetRecordAsString(504);

        private string GetRecordAsString(uint recordType)
        {
            var record = GetRecord(recordType);
            return record != null ? GetDataAsUtf8(record.RecordData) : default!;
        }

        private uint GetRecordAsUint(uint recordType)
        {
            var record = GetRecord(recordType);
            return record != null ? GetDataAsUint(record.RecordData) : uint.MaxValue;
        }

        private EXTHRecord? GetRecord(uint recordType)
        {
            return _recordList.FirstOrDefault(rec => rec.RecordType == recordType);
        }
    }
}
