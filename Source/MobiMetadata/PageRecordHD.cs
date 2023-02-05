namespace MobiMetadata
{
    public class PageRecordHD : PageRecord
    {
        public PageRecordHD(Stream stream, long pos, uint len) : base(stream, pos, len)
        {
        }

        public override async Task WriteDataAsync(params Stream[] streams)
        {
            if (!await WriteDataCoreAsync(RecordId.CRES, streams).ConfigureAwait(false))
            {
                throw new InvalidOperationException("Attempt to write HD image file without CRES marker");
            }
        }

        protected override int GetMagic()
        {
            // Take into account the length between the CRES marker and the start of the HD image.
            return 12;
        }
    }
}
