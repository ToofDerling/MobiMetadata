namespace MobiMetadata
{
    public class MobiHead : BaseHead
    {
        private static readonly List<Attr> _mobiHeadAttrs = new();

        private static readonly Attr _identifierAttr = new(4, _mobiHeadAttrs);

        private static readonly Attr _headerLengthAttr = new(4, _mobiHeadAttrs);

        private static readonly Attr _mobiTypeAttr = new(4, _mobiHeadAttrs);

        private static readonly Attr _textEncodingAttr = new(4, _mobiHeadAttrs);

        private static readonly Attr _uniqueIDAttr = new(4, _mobiHeadAttrs);

        private static readonly Attr _fileVersionAttr = new(4, _mobiHeadAttrs);

        private static readonly Attr _orthographicIndexAttr = new(4, _mobiHeadAttrs);

        private static readonly Attr _inflectionIndexAttr = new(4, _mobiHeadAttrs);

        private static readonly Attr _indexNamesAttr = new(4, _mobiHeadAttrs);

        private static readonly Attr _indexKeysAttr = new(4, _mobiHeadAttrs);

        private static readonly Attr _extraIndex0Attr = new(4, _mobiHeadAttrs);

        private static readonly Attr _extraIndex1Attr = new(4, _mobiHeadAttrs);

        private static readonly Attr _extraIndex2Attr = new(4, _mobiHeadAttrs);

        private static readonly Attr _extraIndex3Attr = new(4, _mobiHeadAttrs);

        private static readonly Attr _extraIndex4Attr = new(4, _mobiHeadAttrs);

        private static readonly Attr _extraIndex5Attr = new(4, _mobiHeadAttrs);

        private static readonly Attr _firstNonBookIndexAttr = new(4, _mobiHeadAttrs);

        private static readonly Attr _fullNameOffsetAttr = new(4, _mobiHeadAttrs);

        private static readonly Attr _fullNameLengthAttr = new(4, _mobiHeadAttrs);

        private static readonly Attr _localeAttr = new(4, _mobiHeadAttrs);

        private static readonly Attr _inputLanguageAttr = new(4, _mobiHeadAttrs);

        private static readonly Attr _outputLanguageAttr = new(4, _mobiHeadAttrs);

        private static readonly Attr _minVersionAttr = new(4, _mobiHeadAttrs);

        private static readonly Attr _firstImageIndexAttr = new(4, _mobiHeadAttrs);

        private static readonly Attr _huffmanRecordOffsetAttr = new(4, _mobiHeadAttrs);

        private static readonly Attr _huffmanRecordCountAttr = new(4, _mobiHeadAttrs);

        private static readonly Attr _huffmanTableOffsetAttr = new(4, _mobiHeadAttrs);

        private static readonly Attr _huffmanTableLengthAttr = new(4, _mobiHeadAttrs);

        private static readonly Attr _exthFlagsAttr = new(4, _mobiHeadAttrs);

        //132	0x84	32	?	32 unknown bytes, if MOBI is long enough
        private static readonly Attr _unknown1Attr = new(32, _mobiHeadAttrs);

        //164	0xa4	4	Unknown Use 0xFFFFFFFF
        private static readonly Attr _unknown2Attr = new(4, _mobiHeadAttrs);

        //168	0xa8	4	DRM Offset  Offset to DRM key info in DRMed files. 0xFFFFFFFF if no DRM
        private static readonly Attr _drmOffsetAttr = new(4, _mobiHeadAttrs);

        //172	0xac	4	DRM Count   Number of entries in DRM info. 0xFFFFFFFF if no DRM
        private static readonly Attr _drmCountAttr = new(4, _mobiHeadAttrs);

        //176	0xb0	4	DRM Size    Number of bytes in DRM info.
        private static readonly Attr _drmSizeAttr = new(4, _mobiHeadAttrs);

        //180	0xb4	4	DRM Flags   Some flags concerning the DRM info.
        private static readonly Attr _drmFlagsAttr = new(4, _mobiHeadAttrs);

        //184	0xb8	8	Unknown Bytes to the end of the MOBI header, including the following if the header length >= 228 (244 from start of record). Use 0x0000000000000000.
        private static readonly Attr _unknown3Attr = new(8, _mobiHeadAttrs);

        //192	0xc0	2	First content record number Number of first text record. Normally 1.
        private static readonly Attr _firstContentRecordNumberAttr = new(2, _mobiHeadAttrs);

        //194	0xc2	2	Last content record number  Number of last image record or number of last text record if it contains no images.Includes Image, DATP, HUFF, DRM.
        private static readonly Attr _lastContentRecordNumberAttr = new(2, _mobiHeadAttrs);

        internal long PreviousHeaderPosition { get; set; }

        public bool SkipExthHeader { get; set; }

        public EXTHHead ExthHeader { get; private set; }

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

            var attrLen = _mobiHeadAttrs.Sum(x => x.Length);
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
            var exthExists = (GetPropAsUint(_exthFlagsAttr) & 0x40) != 0;
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

        //Properties
        public int ExthHeaderSize => ExthHeader == null ? -1 : ExthHeader.Size;

        public string FullName => GetDataAsUtf8(FullNameData);

        public string IdentifierAsString => GetPropAsUtf8RemoveNull(_identifierAttr);

        public uint HeaderLength => GetPropAsUint(_headerLengthAttr);

        public uint FirstImageIndex => GetPropAsUint(_firstImageIndexAttr);

        public uint MobiType => GetPropAsUint(_mobiTypeAttr);

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

        public uint TextEncoding => GetPropAsUint(_textEncodingAttr);

        public string TextEncodingAsString => TextEncoding switch
        {
            1252 => "Cp1252",
            65001 => "UTF-8",
            _ => null!,
        };

        public uint UniqueID => GetPropAsUint(_uniqueIDAttr);

        public uint FileVersion => GetPropAsUint(_fileVersionAttr);

        public uint OrthographicIndex => GetPropAsUint(_orthographicIndexAttr);

        public uint InflectionIndex => GetPropAsUint(_inflectionIndexAttr);

        public uint IndexNames => GetPropAsUint(_indexNamesAttr);

        public uint IndexKeys => GetPropAsUint(_indexKeysAttr);

        public uint ExtraIndex0 => GetPropAsUint(_extraIndex0Attr);

        public uint ExtraIndex1 => GetPropAsUint(_extraIndex1Attr);

        public uint ExtraIndex2 => GetPropAsUint(_extraIndex2Attr);

        public uint ExtraIndex3 => GetPropAsUint(_extraIndex3Attr);

        public uint ExtraIndex4 => GetPropAsUint(_extraIndex4Attr);

        public uint ExtraIndex5 => GetPropAsUint(_extraIndex5Attr);

        public uint FirstNonBookIndex => GetPropAsUint(_firstNonBookIndexAttr);

        public uint FullNameOffset => GetPropAsUint(_fullNameOffsetAttr);

        public uint FullNameLength => GetPropAsUint(_fullNameLengthAttr);

        public uint MinVersion => GetPropAsUint(_minVersionAttr);

        public uint HuffmanRecordOffset => GetPropAsUint(_huffmanRecordOffsetAttr);

        public uint HuffmanRecordCount => GetPropAsUint(_huffmanRecordCountAttr);

        public uint HuffmanTableOffset => GetPropAsUint(_huffmanTableOffsetAttr);

        public uint HuffmanTableLength => GetPropAsUint(_huffmanTableLengthAttr);

        public ushort FirstContentRecordNumber => GetPropAsUshort(_firstContentRecordNumberAttr);

        public ushort LastContentRecordNumber => GetPropAsUshort(_lastContentRecordNumberAttr);
    }
}
