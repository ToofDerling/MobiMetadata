namespace MobiMetadata
{
    public class PalmDOCHead : BaseHead
    {
        private static readonly List<Attr> palmDocHeadAttrs = new();

        private static Attr CompressionAttr => new(2, palmDocHeadAttrs);

        private static Attr Unused0Attr => new(2, palmDocHeadAttrs);

        private static Attr TextLengthAttr => new(4, palmDocHeadAttrs);

        private static Attr RecordCountAttr => new(2, palmDocHeadAttrs);

        private static Attr RecordSizeAttr => new(2, palmDocHeadAttrs);

        private static Attr EncryptionTypeAttr => new(2, palmDocHeadAttrs);

        private static Attr Unused1Attr => new(2, palmDocHeadAttrs);

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
            await SkipOrReadAsync(stream, attrLen);
        }

        //Properties
        public ushort Compression => Converter.ToUInt16(GetPropData(CompressionAttr).Span);

        public string CompressionAsString => Compression switch
        {
            1 => "None",
            2 => "PalmDOC",
            17480 => "HUFF/CDIC",
            _ => $"Unknown (0)",
        };

        public uint TextLength => Converter.ToUInt32(GetPropData(TextLengthAttr).Span);

        public ushort RecordCount => Converter.ToUInt16(GetPropData(RecordCountAttr).Span);

        public ushort RecordSize => Converter.ToUInt16(GetPropData(RecordSizeAttr).Span);

        public ushort EncryptionType => Converter.ToUInt16(GetPropData(EncryptionTypeAttr).Span);

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
                        return $"Unknown (0)";
                }
            }
        }
    }
}
