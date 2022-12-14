using System.Text;

namespace MobiMetadata
{
    public abstract class BaseHead
    {
        public sealed class Attr
        {
            public Attr(int length, List<Attr> attrs = null!)
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

        protected async Task SkipOrReadHeaderDataAsync(Stream stream, int length)
        {
            if (SkipProperties)
            {
                stream.Position += length;
            }
            else
            {
                await ReadHeaderDataAsync(stream, length).ConfigureAwait(false);
            }
        }

        protected async Task ReadHeaderDataAsync(Stream stream, int length)
        {
            HeaderData = new byte[length];
            await stream.ReadAsync(HeaderData).ConfigureAwait(false);
        }

        protected Memory<byte> GetPropData(Attr attr)
        {
            return attr.GetData(HeaderData);
        }

        protected string GetPropAsUtf8(Attr attr)
        {
            return GetDataAsUtf8(GetPropData(attr));
        }

        protected string GetPropAsUtf8RemoveNull(Attr attr)
        {
            return GetDataAsUtf8(GetPropData(attr)).Replace("\0", null);
        }

        protected string GetDataAsUtf8(Memory<byte> memory)
        {
            return Encoding.UTF8.GetString(memory.Span);
        }

        protected uint GetPropAsUint(Attr attr)
        {
            return GetDataAsUint(GetPropData(attr));
        }

        protected uint GetDataAsUint(Memory<byte> memory)
        {
            return Converter.ToUInt32(memory.Span);
        }

        protected ushort GetPropAsUshort(Attr attr)
        {
            return GetDataAsUshort(GetPropData(attr));
        }

        protected ushort GetDataAsUshort(Memory<byte> memory)
        {
            return Converter.ToUInt16(memory.Span);
        }
    }
}
