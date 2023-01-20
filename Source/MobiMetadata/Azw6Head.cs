namespace MobiMetadata
{
    public class Azw6Head : BaseHead
    {
        /* Azw6HeaderInfo from 
           https://github.com/Aeroblast/UnpackKindleS/blob/master/src/Structs%26Dictionary.cs
        4 public UInt32 title_length;
        8 public UInt32 title_offset;
        12 public UInt32 unknown2;
        16 public UInt32 offset_to_hrefs;
        20 public UInt32 num_wo_placeholders;
        24 public UInt32 num_resc_recs;
        28 public UInt32 unknown1;
        32 public UInt32 unknown0;
        36 public UInt32 codepage;
        38 public UInt16 count;
        40 public UInt16 type;
        44 public UInt32 record_size;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        48 public byte[] magic;
        */
        private static readonly List<Attr> _azw6HeadAttrs = new();

        private static readonly Attr _identifierAttr = new(4, _azw6HeadAttrs);

        private static readonly Attr _headerLengthAttr = new(4, _azw6HeadAttrs);

        private static readonly Attr _typeAttr = new(2, _azw6HeadAttrs);
        private static readonly Attr _countAttr = new(2, _azw6HeadAttrs);

        private static readonly Attr _codepageAttr = new(4, _azw6HeadAttrs);

        private static readonly Attr _unknown0Attr = new(4, _azw6HeadAttrs);
        private static readonly Attr _unknown1Attr = new(4, _azw6HeadAttrs);

        private static readonly Attr _rescRecordsCountAttr = new(4, _azw6HeadAttrs);
        private static readonly Attr _rescRecordsWithoutPlaceholdersCountAttr = new(4, _azw6HeadAttrs);

        private static readonly Attr _offsetToHrefsAttr = new(4, _azw6HeadAttrs);

        private static readonly Attr _unknown2Attr = new(4, _azw6HeadAttrs);

        private static readonly Attr _titleOffsetAttr = new(4, _azw6HeadAttrs);
        private static readonly Attr _titleLengthAttr = new(4, _azw6HeadAttrs);

        public bool SkipExthHeader { get; set; }

        public EXTHHead ExthHeader { get; private set; }

        private Memory<byte> TitleData { get; set; }

        /// <summary>
        /// Azw6Head is itself the first pdbrecord in a HD container. It contains an ExthRecord.
        /// SkipProperties is ignored.
        /// </summary>
        /// <param name="skipProperties"></param>
        /// <param name="skipRecords"></param>
        public Azw6Head(bool skipProperties = false, bool skipRecords = false, bool skipExthHeader = false)
        {
            SkipProperties = skipProperties;
            SkipRecords = skipRecords;

            SkipExthHeader = skipExthHeader;
        }

        public override async Task ReadHeaderAsync(Stream stream)
        {
            var attrLen = _azw6HeadAttrs.Sum(x => x.Length);

            await SkipOrReadHeaderDataAsync(stream, attrLen).ConfigureAwait(false);

            if (!SkipExthHeader)
            {
                await ReadExthHeaderAsync(stream).ConfigureAwait(false);
            }

            await ReadTitleAsync(stream).ConfigureAwait(false);
        }

        private async Task ReadExthHeaderAsync(Stream stream)
        {
            // The EXTH header immediately follows the Azw6Header
            ExthHeader = new EXTHHead();
            await ExthHeader.ReadHeaderAsync(stream).ConfigureAwait(false);
        }

        private async Task ReadTitleAsync(Stream stream)
        {
            var headerDataLen = HeaderData.Length;
            if (ExthHeader != null)
            {
                headerDataLen += (int)ExthHeader.HeaderLength;
            }

            var offset = TitleOffset - headerDataLen;
            stream.Position += offset;

            var titleLength = (int)TitleLength;
            TitleData = new byte[titleLength];

            await stream.ReadAsync(TitleData).ConfigureAwait(false);
        }

        //Properties
        public string Title => GetDataAsUtf8(TitleData);

        public string IdentifierAsString => GetPropAsUtf8RemoveNull(_identifierAttr);

        public uint HeaderLength => GetPropAsUint(_headerLengthAttr);

        public uint TitleLength => GetPropAsUint(_titleLengthAttr);

        public uint TitleOffset => GetPropAsUint(_titleOffsetAttr);

        public uint Codepage => GetPropAsUint(_codepageAttr);

        public uint RescRecordsCount => GetPropAsUint(_rescRecordsCountAttr);

        public uint RescRecordsWithoutPlaceholdersCount => GetPropAsUint(_rescRecordsWithoutPlaceholdersCountAttr);
    }
}
