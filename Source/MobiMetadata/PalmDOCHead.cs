namespace MobiMetadata
{
    public class PalmDOCHead : BaseHead
    {
        private static readonly List<Attr> palmDocHeadAttrs = new();

        private static readonly Attr CompressionAttr = new(2, palmDocHeadAttrs);

        private static readonly Attr Unused0Attr = new(2, palmDocHeadAttrs);

        private static readonly Attr TextLengthAttr = new(4, palmDocHeadAttrs);

        private static readonly Attr RecordCountAttr = new(2, palmDocHeadAttrs);

        private static readonly Attr RecordSizeAttr = new(2, palmDocHeadAttrs);

        private static readonly Attr EncryptionTypeAttr = new(2, palmDocHeadAttrs);

        private static readonly Attr Unused1Attr = new(2, palmDocHeadAttrs);

        public long Position { get; private set; }

        public PalmDOCHead(bool skipProperties = false, bool skipRecords = false)
        {
            SkipProperties = skipProperties;
            SkipRecords = skipRecords;
        }

        internal override async Task ReadHeaderAsync(Stream stream)
        {
            Position = stream.Position;

            var attrLen = palmDocHeadAttrs.Sum(x => x.Length);
            await SkipOrReadHeaderDataAsync(stream, attrLen).ConfigureAwait(false);
        }

        //Properties
        public ushort Compression => GetPropAsUshort(CompressionAttr);

        public string CompressionAsString => Compression switch
        {
            1 => "None",
            2 => "PalmDOC",
            17480 => "HUFF/CDIC",
            _ => $"Unknown",
        };

        public uint TextLength => GetPropAsUint(TextLengthAttr);

        public ushort RecordCount => GetPropAsUshort(RecordCountAttr);

        public ushort RecordSize => GetPropAsUshort(RecordSizeAttr);

        public ushort EncryptionType => GetPropAsUshort(EncryptionTypeAttr);

        public string EncryptionTypeAsString
        {
            get
            {
                switch (EncryptionType)
                {
                    case 0: return "None";
                    case 1: return "Old Mobipocket";
                    case 2: return "Mobipocket"; ;
                    default:
                        return $"Unknown";
                }
            }
        }
    }
}
