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
            await SkipOrReadHeaderDataAsync(stream, attrLen);

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

        public string Name => GetPropAsUtf8RemoveNull(NameAttr);

        public ushort Attributes => GetPropAsUshort(AttributesAttr);

        public ushort Version => GetPropAsUshort(VersionAttr);

        public uint CreationDate => GetPropAsUint(CreationDateAttr);

        public uint ModificationDate => GetPropAsUint(ModificationDateAttr);

        public uint LastBackupDate => GetPropAsUint(LastBackupDateAttr);

        public uint ModificationNumber => GetPropAsUint(ModificationNumberAttr);

        public uint AppInfoID => GetPropAsUint(AppInfoIDAttr);

        public uint SortInfoID => GetPropAsUint(SortInfoIDAttr);

        public uint Type => GetPropAsUint(TypeAttr);

        public string TypeAsString => GetPropAsUtf8RemoveNull(TypeAttr);

        public uint Creator => GetPropAsUint(CreatorAttr);

        public string CreatorAsString => GetPropAsUtf8RemoveNull(CreatorAttr);

        public uint UniqueIDSeed => GetPropAsUint(UniqueIDSeedAttr);

        public ushort NumRecords => GetDataAsUshort(NumRecordsData);

        //public ushort GapToData => Converter.ToUInt16(GapToDataAttr.GetData(HeaderData).Span);

        public uint MobiHeaderSize => _recordInfoList.Length > 1
                                        ? _recordInfoList[1].RecordDataOffset - _recordInfoList[0].RecordDataOffset
                                        : 0;
    }
}
