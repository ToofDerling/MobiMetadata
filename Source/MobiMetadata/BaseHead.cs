namespace MobiMetadata
{
    public abstract class BaseHead
    {
        public sealed class Attr
        {
            public Attr(int length)
            {
                Length = length;
            }

            public Attr(int length, int exthRecType) : this(length)
            {
                ExthRecType = exthRecType;
            }

            public byte[]? Data { get; set; }

            public int Length { get; private set; }

            public int ExthRecType { get; private set; }
        }

        private Dictionary<Attr, object>? attrsToRead;

        protected Dictionary<int, object> GetExthRecordTypesToRead()
        {
            if (attrsToRead == null)
            {
                return null;
            }

            return attrsToRead.ToDictionary(x => x.Key.ExthRecType, x => (object)null);
        }

        protected async Task ReadOrSkipAsync(Stream stream, Attr attr)
        {
            if (IsAttrToRead(attr))
            {
                await ReadAsync(stream, attr);
            }
            else
            {
                Skip(stream, attr);
            }
        }

        protected async Task ReadAsync(Stream stream, Attr attr)
        {
            attr.Data = new byte[attr.Length];
            await stream.ReadAsync(attr.Data);
        }

        protected void Skip(Stream stream, Attr attr)
        {
            stream.Position += attr.Length;
        }

        internal void SetAttrsToRead(params Attr[] attrs)
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

        internal bool IsReadAll => attrsToRead == null;

        protected bool IsAttrToRead(Attr attr) => attrsToRead == null || attrsToRead.ContainsKey(attr);

        internal abstract Task ReadHeaderAsync(Stream stream);
    }
}
