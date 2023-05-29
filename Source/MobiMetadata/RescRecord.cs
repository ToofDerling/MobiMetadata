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

        private string _xmlStr;

        public int PageCount { get; private set; }

        public void AdjustPageCountBy(int count)
        {
            PageCount += count;
        }

        public RescRecord(Stream stream, long pos, uint len)
            : base(stream, pos, len)
        { }

        public async Task<string> GetPrettyPrintXmlAsync()
        {
            var sb = new StringBuilder();

            using (var xmlReader = XmlReader.Create(new StringReader(_xmlStr), XmlReaderSettings))
            using (var xmlWriter = XmlWriter.Create(sb, XmlWriterSettings))
            {
                await xmlWriter.WriteNodeAsync(xmlReader, false).ConfigureAwait(false);
            }
            return sb.ToString();
        }

        public async Task ParseXmlAsync()
        {
            var len = _len - RecordId.RESC.Length;

            var data = await ReadDataAsync(len).ConfigureAwait(false);

            var xmlStr = Encoding.UTF8.GetString(data.Span);
            xmlStr = xmlStr.Replace("\0", null).Trim();

            var xmlBegin = xmlStr.IndexOf("<");
            var xmlEnd = xmlStr.LastIndexOf(">") + 1;

            _xmlStr = xmlStr[xmlBegin..xmlEnd];

            int pageCount = 0;
            var strReader = new StringReader(_xmlStr);

            using var xmlReader = XmlReader.Create(strReader, XmlReaderSettings);
            await xmlReader.ReadAsync().ConfigureAwait(false);

            while (await xmlReader.ReadAsync().ConfigureAwait(false))
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

            PageCount = pageCount;
        }
    }
}
