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

        /// <summary>
        /// The Azw6Header is read when PageRecordsHD is not null, ie when we're processing
        /// a HD image container in a azw6 or azw.res file.
        /// </summary>
        public Azw6Head? Azw6Header { get; private set; }

        private readonly bool _throwIfNoExthHeader;

        public MobiMetadata(PDBHead pdbHeader = null, PalmDOCHead palmDocHeader = null, MobiHead mobiHeader = null,
            bool throwIfNoExthHeader = false)
        {
            _pdbHeader = pdbHeader ?? new PDBHead();

            _palmDocHeader = palmDocHeader ?? new PalmDOCHead();

            _mobiHeader = mobiHeader ?? new MobiHead();

            _throwIfNoExthHeader = throwIfNoExthHeader;
        }

        private Stream _azwStream;

        public async Task ReadMetadataAsync(Stream azwStream)
        {
            _azwStream = azwStream;

            await _pdbHeader.ReadHeaderAsync(azwStream).ConfigureAwait(false);

            await _palmDocHeader.ReadHeaderAsync(azwStream).ConfigureAwait(false);
            _mobiHeader.PreviousHeaderPosition = _palmDocHeader.Position;

            // This also reads the exthheader
            await _mobiHeader.ReadHeaderAsync(azwStream).ConfigureAwait(false);

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

            if (MobiHeader.SkipProperties)
            {
                throw new MobiMetadataException($"Cannot read image records ({nameof(MobiHeader.SkipProperties)} is {MobiHeader.SkipProperties}).");
            }

            if (MobiHeader.SkipExthHeader)
            {
                throw new MobiMetadataException($"Cannot read image records ({nameof(MobiHeader.SkipExthHeader)} is {MobiHeader.SkipExthHeader}).");
            }

            var coverIndexOffset = _mobiHeader.ExthHeader.CoverOffset;
            var thumbIndexOffset = _mobiHeader.ExthHeader.ThumbOffset;

            PageRecords = new PageRecords(_azwStream, _pdbHeader.Records, ImageType.SD,
                _mobiHeader.FirstImageIndex, _mobiHeader.LastContentRecordNumber,
                coverIndexOffset, thumbIndexOffset);

            await PageRecords.AnalyzePageRecordsAsync().ConfigureAwait(false);
        }

        public async Task ReadHDImageRecordsAsync(Stream hdContainerStream)
        {
            var pdbHeader = new PDBHead();
            await pdbHeader.ReadHeaderAsync(hdContainerStream).ConfigureAwait(false);

            if (!pdbHeader.IsHDImageContainer)
            {
                throw new MobiMetadataException("Not a HD image container");
            }

            // The azw6 header is the first pdb record 
            Azw6Header = new Azw6Head(skipExthHeader: true);
            await Azw6Header.ReadHeaderAsync(hdContainerStream).ConfigureAwait(false);

            PageRecordsHD = new PageRecords(hdContainerStream, pdbHeader.Records, ImageType.HD,
                1, (ushort)(pdbHeader.Records.Length - 1),
                MobiHeader.ExthHeader.CoverOffset, MobiHeader.ExthHeader.ThumbOffset);

            await PageRecordsHD.AnalyzePageRecordsHDAsync(PageRecords.ContentRecords.Count).ConfigureAwait(false);
        }
    }
}
