using System.Text;

namespace MobiMetadata
{
    public class MobiHead : BaseHead
    {
        private static readonly List<Attr> mobiHeadAttrs = new();

        private static readonly Attr IdentifierAttr = new(4, mobiHeadAttrs);

        public static readonly Attr HeaderLengthAttr = new(4, mobiHeadAttrs);

        public static readonly Attr MobiTypeAttr = new(4, mobiHeadAttrs);

        public static readonly Attr TextEncodingAttr = new(4, mobiHeadAttrs);

        public static readonly Attr UniqueIDAttr = new(4, mobiHeadAttrs);

        public static readonly Attr FileVersionAttr = new(4, mobiHeadAttrs);

        public static readonly Attr OrthographicIndexAttr = new(4, mobiHeadAttrs);

        public static readonly Attr InflectionIndexAttr = new(4, mobiHeadAttrs);

        public static readonly Attr IndexNamesAttr = new(4, mobiHeadAttrs);

        public static readonly Attr IndexKeysAttr = new(4, mobiHeadAttrs);

        public static readonly Attr ExtraIndex0Attr = new(4, mobiHeadAttrs);

        public static readonly Attr ExtraIndex1Attr = new(4, mobiHeadAttrs);

        public static readonly Attr ExtraIndex2Attr = new(4, mobiHeadAttrs);

        public static readonly Attr ExtraIndex3Attr = new(4, mobiHeadAttrs);

        public static readonly Attr ExtraIndex4Attr = new(4, mobiHeadAttrs);

        public static readonly Attr ExtraIndex5Attr = new(4, mobiHeadAttrs);

        public static readonly Attr FirstNonBookIndexAttr = new(4, mobiHeadAttrs);

        public static readonly Attr FullNameOffsetAttr = new(4, mobiHeadAttrs);

        public static readonly Attr FullNameLengthAttr = new(4, mobiHeadAttrs);

        public static readonly Attr LocaleAttr = new(4, mobiHeadAttrs);

        public static readonly Attr InputLanguageAttr = new(4, mobiHeadAttrs);

        public static readonly Attr OutputLanguageAttr = new(4, mobiHeadAttrs);

        public static readonly Attr MinVersionAttr = new(4, mobiHeadAttrs);

        public static readonly Attr FirstImageIndexAttr = new(4, mobiHeadAttrs);

        public static readonly Attr HuffmanRecordOffsetAttr = new(4, mobiHeadAttrs);

        public static readonly Attr HuffmanRecordCountAttr = new(4, mobiHeadAttrs);

        public static readonly Attr HuffmanTableOffsetAttr = new(4, mobiHeadAttrs);

        public static readonly Attr HuffmanTableLengthAttr = new(4, mobiHeadAttrs);

        public static readonly Attr ExthFlagsAttr = new(4, mobiHeadAttrs);

        //132	0x84	32	?	32 unknown bytes, if MOBI is long enough
        public static readonly Attr Unknown1Attr = new(32, mobiHeadAttrs);

        //164	0xa4	4	Unknown Use 0xFFFFFFFF
        public static readonly Attr Unknown2Attr = new(4, mobiHeadAttrs);

        //168	0xa8	4	DRM Offset  Offset to DRM key info in DRMed files. 0xFFFFFFFF if no DRM
        public static readonly Attr DrmOffsetAttr = new(4, mobiHeadAttrs);

        //172	0xac	4	DRM Count   Number of entries in DRM info. 0xFFFFFFFF if no DRM
        public static readonly Attr DrmCountAttr = new(4, mobiHeadAttrs);

        //176	0xb0	4	DRM Size    Number of bytes in DRM info.
        public static readonly Attr DrmSizeAttr = new(4, mobiHeadAttrs);

        //180	0xb4	4	DRM Flags   Some flags concerning the DRM info.
        public static readonly Attr DrmFlagsAttr = new(4, mobiHeadAttrs);

        //184	0xb8	8	Unknown Bytes to the end of the MOBI header, including the following if the header length >= 228 (244 from start of record). Use 0x0000000000000000.
        public static readonly Attr Unknown3Attr = new(8, mobiHeadAttrs);

        //192	0xc0	2	First content record number Number of first text record. Normally 1.
        public static readonly Attr FirstContentRecordNumberAttr = new(2, mobiHeadAttrs);

        //194	0xc2	2	Last content record number  Number of last image record or number of last text record if it contains no images.Includes Image, DATP, HUFF, DRM.
        public static readonly Attr LastContentRecordNumberAttr = new(2, mobiHeadAttrs);

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
            await SkipOrReadAsync(stream, attrLen);

            var snal = stream.Position;

            if (IdentifierAsString != "MOBI")
            {
                throw new MobiMetadataException("Did not get expected MOBI identifier");
            }

            if (!SkipExthHeader)
            {
                await ReadExthHeaderAsync(stream, mobiHeaderOffset);
            }
            
            if (!SkipProperties)
            {
                await ReadFullNameAsync(stream);
            }

            var fullname = FullName;
        }

        private async Task ReadFullNameAsync(Stream stream)
        {
            //Read the fullname
            var rufus = PreviousHeaderPosition;

            var fullnamePos = rufus + FullNameOffset;
            stream.Position = fullnamePos;

            var fullnameLength = (int)FullNameLength;

            FullNameData = new byte[fullnameLength];

            await stream.ReadAsync(FullNameData);
        }

        private async Task ReadExthHeaderAsync(Stream stream, long mobiHeaderOffset)
        {
            //If bit 6 (0x40) is set, then there's an EXTH record 
            bool exthExists = (Converter.ToUInt32(GetPropData(ExthFlagsAttr).Span) & 0x40) != 0;
            if (exthExists)
            {
                // The EXTH header immediately follows the EXTH header, but as the MOBI header is of
                // variable length, we have to calculate the EXTH header offset.
                var exthOffset = mobiHeaderOffset + HeaderLength;
                stream.Position = exthOffset;

                await ExthHeader.ReadHeaderAsync(stream);
            }
        }

        internal void SetExthHeader(EXTHHead exthHeader)
        {
            ExthHeader = exthHeader ?? new EXTHHead();
        }

        public EXTHHead ExthHeader { get; private set; }

        //Properties
        public int ExthHeaderSize => ExthHeader == null ? -1 : ExthHeader.Size;

        public string FullName => Encoding.UTF8.GetString(FullNameData.Span);

        public string IdentifierAsString => Encoding.UTF8.GetString(GetPropData(IdentifierAttr).Span).Replace("\0", string.Empty);

        public uint HeaderLength => Converter.ToUInt32(GetPropData(HeaderLengthAttr).Span);

        public uint FirstImageIndex => Converter.ToUInt32(GetPropData(FirstImageIndexAttr).Span);

        public uint MobiType => Converter.ToUInt32(GetPropData(MobiTypeAttr).Span);

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
            _ => $"Unknown (0)",
        };

        public uint TextEncoding => Converter.ToUInt32(GetPropData(TextEncodingAttr).Span);

        public string TextEncodingAsString => TextEncoding switch
        {
            1252 => "Cp1252",
            65001 => "UTF-8",
            _ => null,
        };

        public uint UniqueID => Converter.ToUInt32(GetPropData(UniqueIDAttr).Span);

        public uint FileVersion => Converter.ToUInt32(GetPropData(FileVersionAttr).Span);

        public uint OrthographicIndex => Converter.ToUInt32(GetPropData(OrthographicIndexAttr).Span);

        public uint InflectionIndex => Converter.ToUInt32(GetPropData(InflectionIndexAttr).Span);

        public uint IndexNames => Converter.ToUInt32(GetPropData(IndexNamesAttr).Span);

        public uint IndexKeys => Converter.ToUInt32(GetPropData(IndexKeysAttr).Span);

        public uint ExtraIndex0 => Converter.ToUInt32(GetPropData(ExtraIndex0Attr).Span);

        public uint ExtraIndex1 => Converter.ToUInt32(GetPropData(ExtraIndex1Attr).Span);

        public uint ExtraIndex2 => Converter.ToUInt32(GetPropData(ExtraIndex2Attr).Span);

        public uint ExtraIndex3 => Converter.ToUInt32(GetPropData(ExtraIndex3Attr).Span);

        public uint ExtraIndex4 => Converter.ToUInt32(GetPropData(ExtraIndex4Attr).Span);

        public uint ExtraIndex5 => Converter.ToUInt32(GetPropData(ExtraIndex5Attr).Span);

        public uint FirstNonBookIndex => Converter.ToUInt32(GetPropData(FirstNonBookIndexAttr).Span);

        public uint FullNameOffset => Converter.ToUInt32(GetPropData(FullNameOffsetAttr).Span);

        public uint FullNameLength => Converter.ToUInt32(GetPropData(FullNameLengthAttr).Span);

        public uint MinVersion => Converter.ToUInt32(GetPropData(MinVersionAttr).Span);

        public uint HuffmanRecordOffset => Converter.ToUInt32(GetPropData(HuffmanRecordOffsetAttr).Span);

        public uint HuffmanRecordCount => Converter.ToUInt32(GetPropData(HuffmanRecordCountAttr).Span);

        public uint HuffmanTableOffset => Converter.ToUInt32(GetPropData(HuffmanTableOffsetAttr).Span);

        public uint HuffmanTableLength => Converter.ToUInt32(GetPropData(HuffmanTableLengthAttr).Span);

        public ushort FirstContentRecordNumber => Converter.ToUInt16(GetPropData(FirstContentRecordNumberAttr).Span);

        public ushort LastContentRecordNumber => Converter.ToUInt16(GetPropData(LastContentRecordNumberAttr).Span);
    }
}
