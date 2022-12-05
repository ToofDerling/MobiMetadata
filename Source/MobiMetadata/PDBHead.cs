using System.Text;

namespace MobiMetadata
{
    public class PDBHead : BaseHead
    {
        public readonly Attr NameAttr = new(32);

        public readonly Attr AttributesAttr = new(2);

        public readonly Attr VersionAttr = new(2);

        public readonly Attr CreationDateAttr = new(4);

        public readonly Attr ModificationDateAttr = new(4);

        public readonly Attr LastBackupDateAttr = new(4);

        public readonly Attr ModificationNumberAttr = new(4);

        public readonly Attr AppInfoIDAttr = new(4);

        public readonly Attr SortInfoIDAttr = new(4);

        public readonly Attr TypeAttr = new(4);

        public readonly Attr CreatorAttr = new(4);

        public readonly Attr UniqueIDSeedAttr = new(4);

        public readonly Attr NextRecordListIDAttr = new(4);

        public readonly Attr NumRecordsAttr = new(2);

        private readonly Attr GapToDataAttr = new(2);

        private PDBRecordInfo[] _recordInfoList;

        public bool RecordInfoIsEmpty { get; set; }

        public PDBRecordInfo[] Records => _recordInfoList;

        internal override void ReadHeader(Stream stream)
        {
            ReadOrSkip(stream, NameAttr);
            ReadOrSkip(stream, AttributesAttr);
            ReadOrSkip(stream, VersionAttr);
            ReadOrSkip(stream, CreationDateAttr);
            ReadOrSkip(stream, ModificationDateAttr);
            ReadOrSkip(stream, LastBackupDateAttr);
            ReadOrSkip(stream, ModificationNumberAttr);
            ReadOrSkip(stream, AppInfoIDAttr);
            ReadOrSkip(stream, SortInfoIDAttr);

            ReadOrSkip(stream, TypeAttr);
            ReadOrSkip(stream, CreatorAttr);
            ReadOrSkip(stream, UniqueIDSeedAttr);
            ReadOrSkip(stream, NextRecordListIDAttr);

            Read(stream, NumRecordsAttr);
            int recordCount = Converter.ToInt16(NumRecordsAttr.Data);

            var readRecordInfo = IsAttrToRead(NumRecordsAttr);
            RecordInfoIsEmpty = !readRecordInfo;

            _recordInfoList = new PDBRecordInfo[recordCount];
            for (int i = 0; i < recordCount; i++)
            {
                _recordInfoList[i] = new PDBRecordInfo(stream, readRecordInfo);
            }

            Skip(stream, GapToDataAttr);
        }

        public bool IsHDImageContainer => TypeAsString == "RBIN" && CreatorAsString == "CONT";

        public string Name => Encoding.ASCII.GetString(NameAttr.Data).Replace("\0", string.Empty);

        public ushort Attributes => Converter.ToUInt16(AttributesAttr.Data);

        public ushort Version => Converter.ToUInt16(VersionAttr.Data);

        public uint CreationDate => Converter.ToUInt32(CreationDateAttr.Data);

        public uint ModificationDate => Converter.ToUInt32(CreationDateAttr.Data);

        public uint LastBackupDate => Converter.ToUInt32(LastBackupDateAttr.Data);

        public uint ModificationNumber => Converter.ToUInt32(ModificationNumberAttr.Data);

        public uint AppInfoID => Converter.ToUInt32(AppInfoIDAttr.Data);

        public uint SortInfoID => Converter.ToUInt32(SortInfoIDAttr.Data);

        public uint Type => Converter.ToUInt32(TypeAttr.Data);

        public string TypeAsString => Encoding.ASCII.GetString(TypeAttr.Data).Replace("\0", string.Empty);

        public uint Creator => Converter.ToUInt32(CreatorAttr.Data);

        public string CreatorAsString => Encoding.ASCII.GetString(CreatorAttr.Data).Replace("\0", string.Empty);

        public uint UniqueIDSeed => Converter.ToUInt32(UniqueIDSeedAttr.Data);

        public ushort NumRecords => Converter.ToUInt16(NumRecordsAttr.Data);

        public ushort GapToData => Converter.ToUInt16(GapToDataAttr.Data);

        public uint MobiHeaderSize => _recordInfoList.Length > 1
                                        ? _recordInfoList[1].RecordDataOffset - _recordInfoList[0].RecordDataOffset
                                        : 0;
    }
}
