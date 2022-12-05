using System.Text;

namespace MobiMetadata
{
    public class EXTHHead : BaseHead
    {
        private readonly Attr IdentifierAttr = new(4);

        public readonly Attr HeaderLengthAttr = new(4);

        public readonly Attr RecordCountAttr = new(4);

        public readonly Attr AuthorAttrAttr = new(0, 100);

        public readonly Attr PublisherAttr = new(0, 101);

        public readonly Attr ImprintAttr = new(0, 102);

        public readonly Attr DescriptionAttr = new(0, 103);

        public readonly Attr IBSNAttr = new(0, 104);

        public readonly Attr SubjectAttr = new(0, 105);

        public readonly Attr PublishedDateAttr = new(0, 106);

        public readonly Attr ReviewAttr = new(0, 107);

        public readonly Attr ContributorAttr = new(0, 108);

        public readonly Attr RightsAttr = new(0, 109);

        public readonly Attr SubjectCodeAttr = new(0, 110);

        public readonly Attr TypeAttr = new(0, 111);

        public readonly Attr SourceAttr = new(0, 112);

        public readonly Attr ASINAttr = new(0, 113);

        public readonly Attr VersionNumberAttr = new(0, 114);

        public readonly Attr RetailPriceAttr = new(0, 118);

        public readonly Attr RetailPriceCurrencyAttr = new(0, 119);

        public readonly Attr Kf8BoundaryOffsetAttr = new(0, 121);

        public readonly Attr BookTypeAttr = new(0, 123);

        public readonly Attr RescOffsetAttr = new(0, 131);

        public readonly Attr DictionaryShortNameAttr = new(0, 200);

        public readonly Attr CoverOffsetAttr = new(0, 201);

        public readonly Attr ThumbOffsetAttr = new(0, 202);

        public readonly Attr HasFakeCoverAttr = new(0, 203);

        public readonly Attr CDETypeAttr = new(0, 501);

        public readonly Attr UpdatedTitleAttr = new(0, 503);

        public readonly Attr ASIN2Attr = new(0, 504);

        private EXTHRecord[] _recordList;

        public override void ReadHeader(Stream stream)
        {
            Read(stream, IdentifierAttr);

            if (IdentifierAsString != "EXTH")
            {
                throw new MobiMetadataException("Did not get expected EXTH identifier");
            }

            ReadOrSkip(stream, HeaderLengthAttr);

            Read(stream, RecordCountAttr);
            var recordTypesToRead = GetExthRecordTypesToRead();

            _recordList = new EXTHRecord[RecordCount];
            for (int i = 0; i < RecordCount; i++)
            {
                _recordList[i] = new EXTHRecord(stream, recordTypesToRead);
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
            int paddingSize = dataSize % 4;

            if (paddingSize != 0)
            {
                paddingSize = 4 - paddingSize;
            }

            return paddingSize;
        }

        //Properties
        public string IdentifierAsString => Encoding.UTF8.GetString(IdentifierAttr.Data).Replace("\0", string.Empty);

        public uint HeaderLength => Converter.ToUInt32(HeaderLengthAttr.Data);

        public uint RecordCount => Converter.ToUInt32(RecordCountAttr.Data);

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
            return record != null ? Encoding.UTF8.GetString(record.RecordData) : default;
        }

        private uint GetRecordAsUint(uint recordType)
        {
            var record = GetRecord(recordType);
            return record != null ? Converter.ToUInt32(record.RecordData) : uint.MaxValue;
        }

        private EXTHRecord GetRecord(uint recordType)
        {
            return _recordList.FirstOrDefault(rec => rec.RecordType == recordType);
        }
    }
}
