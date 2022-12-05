namespace MobiMetadata
{
    public class PalmDOCHead : BaseHead
    {
        public readonly Attr CompressionAttr = new(2);

        public readonly Attr Unused0Attr = new(2);
        
        public readonly Attr TextLengthAttr = new(4);
        
        public readonly Attr RecordCountAttr = new(2);
        
        public readonly Attr RecordSizeAttr = new(2);
        
        public readonly Attr EncryptionTypeAttr = new(2);
        
        public readonly Attr Unused1Attr = new(2);

        public long Position { get; private set; }

        internal override void ReadHeader(Stream stream)
        {
            Position = stream.Position;

            ReadOrSkip(stream, CompressionAttr);
            Skip(stream, Unused0Attr);
            ReadOrSkip(stream, TextLengthAttr);
            ReadOrSkip(stream, RecordCountAttr);

            ReadOrSkip(stream, RecordSizeAttr);
            ReadOrSkip(stream, EncryptionTypeAttr);
            Skip(stream, Unused1Attr);
        }

        //Properties
        public ushort Compression => Converter.ToUInt16(CompressionAttr.Data);

        public string CompressionAsString => Compression switch
        {
            1 => "None",
            2 => "PalmDOC",
            17480 => "HUFF/CDIC",
            _ => $"Unknown (0)",
        };

        public uint TextLength => Converter.ToUInt32(TextLengthAttr.Data);

        public ushort RecordCount => Converter.ToUInt16(RecordCountAttr.Data);

        public ushort RecordSize => Converter.ToUInt16(RecordSizeAttr.Data);

        public ushort EncryptionType => Converter.ToUInt16(EncryptionTypeAttr.Data);

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
