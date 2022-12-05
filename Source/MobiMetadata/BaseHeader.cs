namespace MobiMetadata
{
    public abstract class BaseHeader
    {
        public sealed class Attr
        {
            public Attr(int length)
            {
                Length = length;
            }

            public byte[]? Data { get; set; }

            public int Length { get; private set; }
        }

        private Dictionary<Attr, object>? attrsToRead;

        protected void ReadOrSkip(Stream stream, Attr attr)
        {
            if (IsAttrToRead(attr))
            {
                Read(stream, attr);
            }
            else
            {
                Skip(stream, attr);
            }
        }

        protected void Read(Stream stream, Attr attr)
        {
            attr.Data = new byte[attr.Length];
            stream.Read(attr.Data, 0, attr.Length);
        }

        protected void Skip(Stream stream, Attr attr)
        { 
            stream.Position += attr.Length;
        }

        public void SetAttrsToRead(params Attr[] attrs)
        {
            attrsToRead = new Dictionary<Attr, object>();

            if (attrs != null)
            {
                foreach (var attr in attrs)
                {
                    attrsToRead[attr] = null;
                }
            }
        }

        public bool IsAttrToRead(Attr attr) => attrsToRead == null || attrsToRead.ContainsKey(attr);

        public abstract void ReadHeader(Stream stream);
    }
}
