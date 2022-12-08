using System.IO;

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

            public Attr(int length, int exthRecType) : this(length)
            {
                ExthRecType = exthRecType;
            }

            public int Position { get; set; }

            public byte[]? Data { get; set; }

            public Memory<byte> GetData(Memory<byte> memory)
            { 
                return memory.Slice(Position, Length);
            }

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

        public bool SkipProperties { get; set; }

        public bool SkipRecords { get; set; }

        internal bool IsReadAll => attrsToRead == null;

        protected bool IsAttrToRead(Attr attr) => attrsToRead == null || attrsToRead.ContainsKey(attr);

        protected virtual int GetAttrLength()
        {
            return 0;
        }

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

                var read = await stream.ReadAsync(HeaderData);
            }
        }

        protected Memory<byte> GetPropData(Attr attr)
        {
            return attr.GetData(HeaderData);
        }
    }
}
