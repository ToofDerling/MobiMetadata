namespace MobiMetadata
{
    public abstract class BaseHead
    {
        public sealed class Attr
        {
            public Attr(int length, List<Attr> attrs = null)
            {
                if (attrs != null)
                {
                    Position = attrs.Sum(a => a.Length);
                    attrs.Add(this);
                }

                Length = length;
            }

            public int Position { get; set; }

            public Memory<byte> GetData(Memory<byte> memory)
            { 
                return memory.Slice(Position, Length);
            }

            public int Length { get; private set; }
        }

        public bool SkipProperties { get; set; }

        public bool SkipRecords { get; set; }

        internal abstract Task ReadHeaderAsync(Stream stream);

        protected Memory<byte> HeaderData { get; set; }

        protected async Task SkipOrReadAsync(Stream stream, int length)
        {
            if (SkipProperties)
            {
                stream.Position += length;
            }
            else
            {
                HeaderData = new byte[length];

                await stream.ReadAsync(HeaderData);
            }
        }

        protected Memory<byte> GetPropData(Attr attr)
        {
            return attr.GetData(HeaderData);
        }
    }
}
