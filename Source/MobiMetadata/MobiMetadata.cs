namespace MobiMetadata
{
    public class MobiMetadata
    {
        private readonly PDBHead _pdbHeader;
        private readonly PalmDOCHead _palmDocHeader;
        private readonly MobiHead _mobiHeader;

        public PDBHead PdbHeader => _pdbHeader;

        public PalmDOCHead PalmDocHeader => _palmDocHeader;

        public MobiHead MobiHeader => _mobiHeader;

        public PageRecords PageRecords { get; private set; }

        public PageRecords? PageRecordsHD { get; private set; }

        private readonly bool _throwIfNoExthHeader;

        public MobiMetadata(PDBHead pdbHeader = null, PalmDOCHead palmDocHeader = null, MobiHead mobiHeader = null,
            EXTHHead exthHeader = null, bool throwIfNoExthHeader = false)
        {
            _pdbHeader = pdbHeader ?? new PDBHead();

            _palmDocHeader = palmDocHeader ?? new PalmDOCHead();

            _mobiHeader = mobiHeader ?? new MobiHead();

            _mobiHeader.SetExthHeader(exthHeader);
            _throwIfNoExthHeader = throwIfNoExthHeader;
        }

        private Stream _stream;

        public async Task ReadMetadataAsync(Stream stream)
        {
            _stream = stream;

            await _pdbHeader.ReadHeaderAsync(stream);

            await _palmDocHeader.ReadHeaderAsync(stream);
            _mobiHeader.PreviousHeaderPosition = _palmDocHeader.Position;

            // This also reads the exthheader
            await _mobiHeader.ReadHeaderAsync(stream);

            if (_mobiHeader.ExthHeader == null && _throwIfNoExthHeader)
            {
                throw new MobiMetadataException($"No EXTHHeader");
            }
        }

        public async Task ReadImageRecordsAsync()
        {
            if (PdbHeader.SkipRecords)
            {
                throw new MobiMetadataException($"Cannot read image records ({nameof(PdbHeader.SkipRecords)} is {PdbHeader.SkipRecords}).");
            }

            var coverIndexOffset = _mobiHeader.ExthHeader.CoverOffset;
            var thumbIndexOffset = _mobiHeader.ExthHeader.ThumbOffset;

            PageRecords = new PageRecords(_stream, _pdbHeader.Records, ImageType.SD,
                _mobiHeader.FirstImageIndex, _mobiHeader.LastContentRecordNumber,
                coverIndexOffset, thumbIndexOffset);

            await PageRecords.AnalyzePageRecordsAsync();
        }

        public async Task ReadHDImageRecordsAsync(Stream hdContainerStream)
        {
            var pdbHeader = new PDBHead();
            await pdbHeader.ReadHeaderAsync(hdContainerStream);

            if (!pdbHeader.IsHDImageContainer)
            {
                throw new MobiMetadataException("Not a HD image container");
            }

            PageRecordsHD = new PageRecords(hdContainerStream, pdbHeader.Records, ImageType.HD,
                1, (ushort)(pdbHeader.Records.Length - 1),
                MobiHeader.ExthHeader.CoverOffset, MobiHeader.ExthHeader.ThumbOffset);

            await PageRecordsHD.AnalyzePageRecordsHDAsync(PageRecords.ContentRecords.Count);
        }
    }
}
