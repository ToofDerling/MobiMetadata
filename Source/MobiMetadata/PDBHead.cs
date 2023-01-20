namespace MobiMetadata
{
    public class PDBHead : BaseHead
    {
        private static readonly List<Attr> _pdbHeadAttrs = new();

        private static readonly Attr _nameAttr = new(32, _pdbHeadAttrs);

        private static readonly Attr _attributesAttr = new(2, _pdbHeadAttrs);

        private static readonly Attr _versionAttr = new(2, _pdbHeadAttrs);

        private static readonly Attr _creationDateAttr = new(4, _pdbHeadAttrs);

        private static readonly Attr _modificationDateAttr = new(4, _pdbHeadAttrs);

        private static readonly Attr _lastBackupDateAttr = new(4, _pdbHeadAttrs);

        private static readonly Attr _modificationNumberAttr = new(4, _pdbHeadAttrs);

        private static readonly Attr _appInfoIDAttr = new(4, _pdbHeadAttrs);

        private static readonly Attr _sortInfoIDAttr = new(4, _pdbHeadAttrs);

        private static readonly Attr _typeAttr = new(4, _pdbHeadAttrs);

        private static readonly Attr _creatorAttr = new(4, _pdbHeadAttrs);

        private static readonly Attr _uniqueIDSeedAttr = new(4, _pdbHeadAttrs);

        private static readonly Attr _nextRecordListIDAttr = new(4, _pdbHeadAttrs);

        private static readonly Attr _numRecordsAttr = new(2);

        private static readonly Attr _gapToDataAttr = new(2);

        private PDBRecordInfo[] _recordInfoList;

        public PDBRecordInfo[] Records => _recordInfoList;

        private Memory<byte> NumRecordsData { get; set; }

        private Memory<byte> RecordsData { get; set; }

        public PDBHead(bool skipProperties = false, bool skipRecords = false)
        {
            SkipProperties = skipProperties;
            SkipRecords = skipRecords;
        }

        public override async Task ReadHeaderAsync(Stream stream)
        {
            var attrLen = _pdbHeadAttrs.Sum(x => x.Length);
            await SkipOrReadHeaderDataAsync(stream, attrLen).ConfigureAwait(false);

            NumRecordsData = new byte[_numRecordsAttr.Length];
            await stream.ReadAsync(NumRecordsData).ConfigureAwait(false);

            var recordCount = NumRecords;
            var recordDataSize = recordCount * PDBRecordInfo.PdbRecordLen;

            if (SkipRecords)
            {
                stream.Position += recordDataSize;
            }
            else
            {
                RecordsData = new byte[recordDataSize];
                await stream.ReadAsync(RecordsData).ConfigureAwait(false);

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
            stream.Position += _gapToDataAttr.Length;
        }

        public bool IsHDImageContainer => TypeAsString == "RBIN" && CreatorAsString == "CONT";

        public string Name => GetPropAsUtf8RemoveNull(_nameAttr);

        public ushort Attributes => GetPropAsUshort(_attributesAttr);

        public ushort Version => GetPropAsUshort(_versionAttr);

        public uint CreationDate => GetPropAsUint(_creationDateAttr);

        public uint ModificationDate => GetPropAsUint(_modificationDateAttr);

        public uint LastBackupDate => GetPropAsUint(_lastBackupDateAttr);

        public uint ModificationNumber => GetPropAsUint(_modificationNumberAttr);

        public uint AppInfoID => GetPropAsUint(_appInfoIDAttr);

        public uint SortInfoID => GetPropAsUint(_sortInfoIDAttr);

        public uint Type => GetPropAsUint(_typeAttr);

        public string TypeAsString => GetPropAsUtf8RemoveNull(_typeAttr);

        public uint Creator => GetPropAsUint(_creatorAttr);

        public string CreatorAsString => GetPropAsUtf8RemoveNull(_creatorAttr);

        public uint UniqueIDSeed => GetPropAsUint(_uniqueIDSeedAttr);

        public ushort NumRecords => GetDataAsUshort(NumRecordsData);

        //public ushort GapToData => Converter.ToUInt16(GapToDataAttr.GetData(HeaderData).Span);

        public uint MobiHeaderSize => _recordInfoList.Length > 1
                                        ? _recordInfoList[1].RecordDataOffset - _recordInfoList[0].RecordDataOffset
                                        : 0;
    }
}
