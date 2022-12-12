namespace MobiMetadata
{
    public class ImageRecordHD : PageRecord
    {
        public ImageRecordHD(Stream stream, long pos, uint len) : base(stream, pos, len)
        {
        }

        protected override int GetMagic()
        {
            // Take into account the length between the CRES marker
            // and the start of the HD image.
            return 12;
        }
    }
}
