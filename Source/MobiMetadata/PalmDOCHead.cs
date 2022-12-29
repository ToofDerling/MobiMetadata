namespace MobiMetadata
{
    public class PalmDOCHead : BaseHead
    {
        private static readonly List<Attr> _palmDocHeadAttrs = new();

        private static readonly Attr _compressionAttr = new(2, _palmDocHeadAttrs);

        private static readonly Attr _unused0Attr = new(2, _palmDocHeadAttrs);

        private static readonly Attr _textLengthAttr = new(4, _palmDocHeadAttrs);

        private static readonly Attr _recordCountAttr = new(2, _palmDocHeadAttrs);

        private static readonly Attr _recordSizeAttr = new(2, _palmDocHeadAttrs);

        private static readonly Attr _encryptionTypeAttr = new(2, _palmDocHeadAttrs);

        private static readonly Attr _unused1Attr = new(2, _palmDocHeadAttrs);

        public long Position { get; private set; }

        public PalmDOCHead(bool skipProperties = false, bool skipRecords = false)
        {
            SkipProperties = skipProperties;
            SkipRecords = skipRecords;
        }

        internal override async Task ReadHeaderAsync(Stream stream)
        {
            Position = stream.Position;

            var attrLen = _palmDocHeadAttrs.Sum(x => x.Length);
            await SkipOrReadHeaderDataAsync(stream, attrLen).ConfigureAwait(false);
        }

        //Properties
        public ushort Compression => GetPropAsUshort(_compressionAttr);

        public string CompressionAsString => Compression switch
        {
            1 => "None",
            2 => "PalmDOC",
            17480 => "HUFF/CDIC",
            _ => $"Unknown",
        };

        public uint TextLength => GetPropAsUint(_textLengthAttr);

        public ushort RecordCount => GetPropAsUshort(_recordCountAttr);

        public ushort RecordSize => GetPropAsUshort(_recordSizeAttr);

        public ushort EncryptionType => GetPropAsUshort(_encryptionTypeAttr);

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
