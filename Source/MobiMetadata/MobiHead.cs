namespace MobiMetadata
{
    public class MobiHead : BaseHead
    {
        private static readonly List<Attr> mobiHeadAttrs = new();

        private static readonly Attr IdentifierAttr = new(4, mobiHeadAttrs);

        private static readonly Attr HeaderLengthAttr = new(4, mobiHeadAttrs);

        private static readonly Attr MobiTypeAttr = new(4, mobiHeadAttrs);

        private static readonly Attr TextEncodingAttr = new(4, mobiHeadAttrs);

        private static readonly Attr UniqueIDAttr = new(4, mobiHeadAttrs);

        private static readonly Attr FileVersionAttr = new(4, mobiHeadAttrs);

        private static readonly Attr OrthographicIndexAttr = new(4, mobiHeadAttrs);

        private static readonly Attr InflectionIndexAttr = new(4, mobiHeadAttrs);

        private static readonly Attr IndexNamesAttr = new(4, mobiHeadAttrs);

        private static readonly Attr IndexKeysAttr = new(4, mobiHeadAttrs);

        private static readonly Attr ExtraIndex0Attr = new(4, mobiHeadAttrs);

        private static readonly Attr ExtraIndex1Attr = new(4, mobiHeadAttrs);

        private static readonly Attr ExtraIndex2Attr = new(4, mobiHeadAttrs);

        private static readonly Attr ExtraIndex3Attr = new(4, mobiHeadAttrs);

        private static readonly Attr ExtraIndex4Attr = new(4, mobiHeadAttrs);

        private static readonly Attr ExtraIndex5Attr = new(4, mobiHeadAttrs);

        private static readonly Attr FirstNonBookIndexAttr = new(4, mobiHeadAttrs);

        private static readonly Attr FullNameOffsetAttr = new(4, mobiHeadAttrs);

        private static readonly Attr FullNameLengthAttr = new(4, mobiHeadAttrs);

        private static readonly Attr LocaleAttr = new(4, mobiHeadAttrs);

        private static readonly Attr InputLanguageAttr = new(4, mobiHeadAttrs);

        private static readonly Attr OutputLanguageAttr = new(4, mobiHeadAttrs);

        private static readonly Attr MinVersionAttr = new(4, mobiHeadAttrs);

        private static readonly Attr FirstImageIndexAttr = new(4, mobiHeadAttrs);

        private static readonly Attr HuffmanRecordOffsetAttr = new(4, mobiHeadAttrs);

        private static readonly Attr HuffmanRecordCountAttr = new(4, mobiHeadAttrs);

        private static readonly Attr HuffmanTableOffsetAttr = new(4, mobiHeadAttrs);

        private static readonly Attr HuffmanTableLengthAttr = new(4, mobiHeadAttrs);

        private static readonly Attr ExthFlagsAttr = new(4, mobiHeadAttrs);

        //132	0x84	32	?	32 unknown bytes, if MOBI is long enough
        private static readonly Attr Unknown1Attr = new(32, mobiHeadAttrs);

        //164	0xa4	4	Unknown Use 0xFFFFFFFF
        private static readonly Attr Unknown2Attr = new(4, mobiHeadAttrs);

        //168	0xa8	4	DRM Offset  Offset to DRM key info in DRMed files. 0xFFFFFFFF if no DRM
        private static readonly Attr DrmOffsetAttr = new(4, mobiHeadAttrs);

        //172	0xac	4	DRM Count   Number of entries in DRM info. 0xFFFFFFFF if no DRM
        private static readonly Attr DrmCountAttr = new(4, mobiHeadAttrs);

        //176	0xb0	4	DRM Size    Number of bytes in DRM info.
        private static readonly Attr DrmSizeAttr = new(4, mobiHeadAttrs);

        //180	0xb4	4	DRM Flags   Some flags concerning the DRM info.
        private static readonly Attr DrmFlagsAttr = new(4, mobiHeadAttrs);

        //184	0xb8	8	Unknown Bytes to the end of the MOBI header, including the following if the header length >= 228 (244 from start of record). Use 0x0000000000000000.
        private static readonly Attr Unknown3Attr = new(8, mobiHeadAttrs);

        //192	0xc0	2	First content record number Number of first text record. Normally 1.
        private static readonly Attr FirstContentRecordNumberAttr = new(2, mobiHeadAttrs);

        //194	0xc2	2	Last content record number  Number of last image record or number of last text record if it contains no images.Includes Image, DATP, HUFF, DRM.
        private static readonly Attr LastContentRecordNumberAttr = new(2, mobiHeadAttrs);

        internal long PreviousHeaderPosition { get; set; }

        public bool SkipExthHeader { get; set; }

        private Memory<byte> FullNameData { get; set; }

        public MobiHead(bool skipProperties = false, bool skipRecords = false, bool skipExthHeader = false)
        {
            SkipProperties = skipProperties;
            SkipRecords = skipRecords;

            SkipExthHeader = skipExthHeader;
            if (!SkipExthHeader)
            {
                SkipProperties = false;
            }
        }

        internal override async Task ReadHeaderAsync(Stream stream)
        {
            var mobiHeaderOffset = stream.Position;

            var attrLen = mobiHeadAttrs.Sum(x => x.Length);
            await SkipOrReadHeaderDataAsync(stream, attrLen).ConfigureAwait(false);

            if (IdentifierAsString != "MOBI")
            {
                throw new MobiMetadataException("Did not get expected MOBI identifier");
            }

            if (!SkipExthHeader)
            {
                await ReadExthHeaderAsync(stream, mobiHeaderOffset).ConfigureAwait(false);
            }
            
            if (!SkipProperties)
            {
                await ReadFullNameAsync(stream).ConfigureAwait(false);
            }
        }

        private async Task ReadFullNameAsync(Stream stream)
        {
            var fullnamePos = PreviousHeaderPosition + FullNameOffset;
            stream.Position = fullnamePos;

            var fullnameLength = (int)FullNameLength;
            FullNameData = new byte[fullnameLength];

            await stream.ReadAsync(FullNameData).ConfigureAwait(false);
        }

        private async Task ReadExthHeaderAsync(Stream stream, long mobiHeaderOffset)
        {
            //If bit 6 (0x40) is set, then there's an EXTH record 
            bool exthExists = (GetPropAsUint(ExthFlagsAttr) & 0x40) != 0;
            if (exthExists)
            {
                // The EXTH header immediately follows the Mobi header, but as the MOBI header is of
                // variable length, we have to calculate the EXTH header offset.
                var exthOffset = mobiHeaderOffset + HeaderLength;
                stream.Position = exthOffset;

                ExthHeader = new EXTHHead();
                await ExthHeader.ReadHeaderAsync(stream).ConfigureAwait(false);
            }
        }

        public EXTHHead ExthHeader { get; private set; }

        //Properties
        public int ExthHeaderSize => ExthHeader == null ? -1 : ExthHeader.Size;

        public string FullName => GetDataAsUtf8(FullNameData);

        public string IdentifierAsString => GetPropAsUtf8RemoveNull(IdentifierAttr);

        public uint HeaderLength => GetPropAsUint(HeaderLengthAttr);

        public uint FirstImageIndex => GetPropAsUint(FirstImageIndexAttr);

        public uint MobiType => GetPropAsUint(MobiTypeAttr);

        public string MobiTypeAsString => MobiType switch
        {
            2 => "Mobipocket Book",
            3 => "PalmDoc Book",
            4 => "Audio",
            257 => "News",
            258 => "News Feed",
            259 => "News Magazine",
            513 => "PICS",
            514 => "WORD",
            515 => "XLS",
            516 => "PPT",
            517 => "TEXT",
            518 => "HTML",
            _ => $"Unknown",
        };

        public uint TextEncoding => GetPropAsUint(TextEncodingAttr);

        public string TextEncodingAsString => TextEncoding switch
        {
            1252 => "Cp1252",
            65001 => "UTF-8",
            _ => null!,
        };

        public uint UniqueID => GetPropAsUint(UniqueIDAttr);

        public uint FileVersion => GetPropAsUint(FileVersionAttr);

        public uint OrthographicIndex => GetPropAsUint(OrthographicIndexAttr);

        public uint InflectionIndex => GetPropAsUint(InflectionIndexAttr);

        public uint IndexNames => GetPropAsUint(IndexNamesAttr);

        public uint IndexKeys => GetPropAsUint(IndexKeysAttr);

        public uint ExtraIndex0 => GetPropAsUint(ExtraIndex0Attr);

        public uint ExtraIndex1 => GetPropAsUint(ExtraIndex1Attr);

        public uint ExtraIndex2 => GetPropAsUint(ExtraIndex2Attr);

        public uint ExtraIndex3 => GetPropAsUint(ExtraIndex3Attr);

        public uint ExtraIndex4 => GetPropAsUint(ExtraIndex4Attr);

        public uint ExtraIndex5 => GetPropAsUint(ExtraIndex5Attr);

        public uint FirstNonBookIndex => GetPropAsUint(FirstNonBookIndexAttr);

        public uint FullNameOffset => GetPropAsUint(FullNameOffsetAttr);

        public uint FullNameLength => GetPropAsUint(FullNameLengthAttr);

        public uint MinVersion => GetPropAsUint(MinVersionAttr);

        public uint HuffmanRecordOffset => GetPropAsUint(HuffmanRecordOffsetAttr);

        public uint HuffmanRecordCount => GetPropAsUint(HuffmanRecordCountAttr);

        public uint HuffmanTableOffset => GetPropAsUint(HuffmanTableOffsetAttr);

        public uint HuffmanTableLength => GetPropAsUint(HuffmanTableLengthAttr);

        public ushort FirstContentRecordNumber => GetPropAsUshort(FirstContentRecordNumberAttr);

        public ushort LastContentRecordNumber => GetPropAsUshort(LastContentRecordNumberAttr);
    }
}
