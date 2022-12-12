using System.Buffers;
using System.Text;
using System.Xml;

namespace MobiMetadata
{
    public class RescRecord : PageRecord
    {
        private static XmlReaderSettings XmlReaderSettings => new()
        {
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true,
            ConformanceLevel = ConformanceLevel.Fragment,
            Async = true,
        };

        private static XmlWriterSettings XmlWriterSettings => new()
        {
            ConformanceLevel = ConformanceLevel.Fragment,
            Indent = true,
            Async = true,
        };

        public string RawXml { get; private set; }

        public int PageCount { get; private set; }

        public void AdjustPageCountBy(int count)
        {
            PageCount += count;
        }

        public RescRecord(Stream stream, long pos, uint len) : base(stream, pos, len)
        { }

        public async Task<string> GetPrettyPrintXmlAsync()
        {
            var sb = new StringBuilder();

            using (var xmlReader = XmlReader.Create(new StringReader(RawXml), XmlReaderSettings))
            using (var xmlWriter = XmlWriter.Create(sb, XmlWriterSettings))
            {
                await xmlWriter.WriteNodeAsync(xmlReader, false);
            }
            return sb.ToString();
        }

        public async Task ParseXmlAsync()
        {
            var len = _len - RecordId.RESC.Length;

            var bytes = ArrayPool<byte>.Shared.Rent(len);
            try
            {
                var data = await ReadDataAsync(bytes, len);

                var xmlStr = Encoding.UTF8.GetString(data.Span);
                xmlStr = xmlStr.Replace("\0", null).Trim();

                var xmlBegin = xmlStr.IndexOf("<");
                var xmlEnd = xmlStr.LastIndexOf(">");

                xmlStr = xmlStr.Substring(xmlBegin, xmlEnd - xmlBegin + 1);

                int pageCount = 0;

                var strReader = new StringReader(xmlStr);

                using (XmlReader xmlReader = XmlReader.Create(strReader, XmlReaderSettings))
                {
                    await xmlReader.ReadAsync();
                    while (await xmlReader.ReadAsync())
                    {
                        if (xmlReader.Name == "itemref")
                        {
                            var idRef = xmlReader.GetAttribute("idref");
                            var skelId = xmlReader.GetAttribute("skelid");

                            if (idRef != null && skelId != null)
                            {
                                pageCount++;
                            }
                        }
                    }
                }

                RawXml = xmlStr;
                PageCount = pageCount;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }

        }
    }
}
