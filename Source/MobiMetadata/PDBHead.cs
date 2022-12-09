using System.Text;

namespace MobiMetadata
{
    public class PDBHead : BaseHead
    {
        private static readonly List<Attr> pdbHeadAttrs = new();

        private static readonly Attr NameAttr = new(32, pdbHeadAttrs);

        private static readonly Attr AttributesAttr = new(2, pdbHeadAttrs);

        private static readonly Attr VersionAttr = new(2, pdbHeadAttrs);

        private static readonly Attr CreationDateAttr = new(4, pdbHeadAttrs);

        private static readonly Attr ModificationDateAttr = new(4, pdbHeadAttrs);

        private static readonly Attr LastBackupDateAttr = new(4, pdbHeadAttrs);

        private static readonly Attr ModificationNumberAttr = new(4, pdbHeadAttrs);

        private static readonly Attr AppInfoIDAttr = new(4, pdbHeadAttrs);

        private static readonly Attr SortInfoIDAttr = new(4, pdbHeadAttrs);

        private static readonly Attr TypeAttr = new(4, pdbHeadAttrs);

        private static readonly Attr CreatorAttr = new(4, pdbHeadAttrs);

        private static readonly Attr UniqueIDSeedAttr = new(4, pdbHeadAttrs);

        private static readonly Attr NextRecordListIDAttr = new(4, pdbHeadAttrs);

        private static readonly Attr NumRecordsAttr = new(2);

        private static readonly Attr GapToDataAttr = new(2);

        private PDBRecordInfo[] _recordInfoList;

        public PDBRecordInfo[] Records => _recordInfoList;

        private Memory<byte> NumRecordsData { get; set; }

        private Memory<byte> RecordsData { get; set; }

        public PDBHead(bool skipProperties = false, bool skipRecords = false)
        {
            SkipProperties = skipProperties;
            SkipRecords = skipRecords;
        }

        internal override async Task ReadHeaderAsync(Stream stream)
        {
            var attrLen = pdbHeadAttrs.Sum(x => x.Length);
            await SkipOrReadAsync(stream, attrLen);

            NumRecordsData = new byte[NumRecordsAttr.Length];
            await stream.ReadAsync(NumRecordsData);

            var recordCount = NumRecords;
            var recordDataSize = recordCount * PDBRecordInfo.PdbRecordLen;

            if (SkipRecords)
            {
                stream.Position += recordDataSize;
            }
            else
            {
                RecordsData = new byte[recordDataSize];
                await stream.ReadAsync(RecordsData);

                _recordInfoList = new PDBRecordInfo[recordCount];
                var recordPos = 0;

                for (int i = 0; i < recordCount; i++)
                {
                    var recordInfo = new PDBRecordInfo(RecordsData, recordPos);
                    _recordInfoList[i] = recordInfo;

                    recordPos += PDBRecordInfo.PdbRecordLen;
                }
            }

            // Finally move on to next header
            stream.Position += GapToDataAttr.Length;
        }

        public bool IsHDImageContainer => TypeAsString == "RBIN" && CreatorAsString == "CONT";

        public string Name => Encoding.ASCII.GetString(GetPropData(NameAttr).Span).Replace("\0", string.Empty);

        public ushort Attributes => Converter.ToUInt16(GetPropData(AttributesAttr).Span);

        public ushort Version => Converter.ToUInt16(GetPropData(VersionAttr).Span);

        public uint CreationDate => Converter.ToUInt32(GetPropData(CreationDateAttr).Span);

        public uint ModificationDate => Converter.ToUInt32(GetPropData(ModificationDateAttr).Span);

        public uint LastBackupDate => Converter.ToUInt32(GetPropData(LastBackupDateAttr).Span);

        public uint ModificationNumber => Converter.ToUInt32(GetPropData(ModificationNumberAttr).Span);

        public uint AppInfoID => Converter.ToUInt32(GetPropData(AppInfoIDAttr).Span);

        public uint SortInfoID => Converter.ToUInt32(GetPropData(SortInfoIDAttr).Span);

        public uint Type => Converter.ToUInt32(GetPropData(TypeAttr).Span);

        public string TypeAsString => Encoding.ASCII.GetString(GetPropData(TypeAttr).Span).Replace("\0", string.Empty);

        public uint Creator => Converter.ToUInt32(GetPropData(CreatorAttr).Span);

        public string CreatorAsString => Encoding.ASCII.GetString(GetPropData(CreatorAttr).Span).Replace("\0", string.Empty);

        public uint UniqueIDSeed => Converter.ToUInt32(GetPropData(UniqueIDSeedAttr).Span);

        public ushort NumRecords => Converter.ToUInt16(NumRecordsData.Span);

        //public ushort GapToData => Converter.ToUInt16(GapToDataAttr.GetData(HeaderData).Span);

        public uint MobiHeaderSize => _recordInfoList.Length > 1
                                        ? _recordInfoList[1].RecordDataOffset - _recordInfoList[0].RecordDataOffset
                                        : 0;
    }
}
