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

        internal override async Task ReadHeaderAsync(Stream stream)
        {
            await ReadOrSkipAsync(stream, NameAttr);
            await ReadOrSkipAsync(stream, AttributesAttr);
            await ReadOrSkipAsync(stream, VersionAttr);
            await ReadOrSkipAsync(stream, CreationDateAttr);
            await ReadOrSkipAsync(stream, ModificationDateAttr);
            await ReadOrSkipAsync(stream, LastBackupDateAttr);
            await ReadOrSkipAsync(stream, ModificationNumberAttr);
            await ReadOrSkipAsync(stream, AppInfoIDAttr);
            await ReadOrSkipAsync(stream, SortInfoIDAttr);

            await ReadOrSkipAsync(stream, TypeAttr);
            await ReadOrSkipAsync(stream, CreatorAttr);
            await ReadOrSkipAsync(stream, UniqueIDSeedAttr);
            await ReadOrSkipAsync(stream, NextRecordListIDAttr);

            await ReadAsync(stream, NumRecordsAttr);
            int recordCount = Converter.ToInt16(NumRecordsAttr.Data);

            var readRecordInfo = IsAttrToRead(NumRecordsAttr);
            RecordInfoIsEmpty = !readRecordInfo;

            _recordInfoList = new PDBRecordInfo[recordCount];
            for (int i = 0; i < recordCount; i++)
            {
                var recordInfo = new PDBRecordInfo(readRecordInfo);
                await recordInfo.ReadRecordInfoAsync(stream);

                _recordInfoList[i] = recordInfo; 
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
