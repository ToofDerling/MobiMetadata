namespace MobiMetadata
{
    public class MobiMetadata
    {
        private readonly PDBHead _pdbHeader;
        private readonly PalmDOCHead _palmDocHeader;
        private readonly MobiHead _mobiHeader;
        private readonly PageRecords pageRecords;

        public PDBHead PDBHeader => _pdbHeader;

        public PalmDOCHead PalmDocHeader => _palmDocHeader;

        public MobiHead MobiHeader => _mobiHeader;

        public PageRecords PageRecords => pageRecords;

        public PageRecords? PageRecordsHD { get; private set; }

        public MobiMetadata(Stream stream, PDBHead pdbHeader = null, PalmDOCHead palmDocHeader = null, MobiHead mobiHeader = null, bool throwIfNoExthHeader = false)
        {
            _pdbHeader = pdbHeader ?? new PDBHead();
            _pdbHeader.ReadHeader(stream);

            _palmDocHeader = palmDocHeader ?? new PalmDOCHead();
            _palmDocHeader.ReadHeader(stream);

            _mobiHeader = mobiHeader ?? new MobiHead();
            _mobiHeader.PreviousHeaderPosition = _palmDocHeader.Position;
            _mobiHeader.ReadHeader(stream);

            if (_mobiHeader.EXTHHeader.IsEmpty)
            {
                if (throwIfNoExthHeader)
                {
                    throw new MobiMetadataException($"{mobiHeader.FullName}: No EXTHHeader");
                }
            }
            else if (!_pdbHeader.RecordInfoIsEmpty) 
            {
                var coverIndexOffset = _mobiHeader.EXTHHeader.CoverOffset;
                var thumbIndexOffset = _mobiHeader.EXTHHeader.ThumbOffset;

                pageRecords = new PageRecords(stream, _pdbHeader.Records, ImageType.SD,
                    _mobiHeader.FirstImageIndex, _mobiHeader.LastContentRecordNumber,
                    coverIndexOffset, thumbIndexOffset);

                pageRecords.AnalyzePageRecords();
            }
        }

        public void ReadHDImageRecords(Stream hdContainerStream)
        {
            var pdbHeader = new PDBHead();

            pdbHeader.SetAttrsToRead(pdbHeader.TypeAttr, pdbHeader.CreatorAttr, pdbHeader.NumRecordsAttr);
            pdbHeader.ReadHeader(hdContainerStream);

            if (!pdbHeader.IsHDImageContainer)
            {
                throw new MobiMetadataException("Not a HD image container");
            }

            PageRecordsHD = new PageRecords(hdContainerStream, pdbHeader.Records, ImageType.HD,
                1, (ushort)(pdbHeader.Records.Length - 1),
                MobiHeader.EXTHHeader.CoverOffset, MobiHeader.EXTHHeader.ThumbOffset);

            PageRecordsHD.AnalyzePageRecordsHD(PageRecords.ContentRecords.Count);
        }
    }
}
