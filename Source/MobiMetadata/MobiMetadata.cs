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
        /// The Azw6Header is read when processing a HD image container in an azw6 or azw.res file.
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

            await PdbHeader.ReadHeaderAsync(azwStream).ConfigureAwait(false);

            await PalmDocHeader.ReadHeaderAsync(azwStream).ConfigureAwait(false);
            MobiHeader.PreviousHeaderPosition = PalmDocHeader.Position;

            // This also reads the exthheader
            await MobiHeader.ReadHeaderAsync(azwStream).ConfigureAwait(false);

            if (MobiHeader.ExthHeader == null && _throwIfNoExthHeader)
            {
                throw new MobiMetadataException($"No EXTHHeader");
            }
        }

        public async Task ReadImageRecordsAsync()
        {
            if (PdbHeader == null || MobiHeader == null)
            {
                throw new MobiMetadataException("Must call ReadMetadataAsync before calling ReadImageRecordsAsync");
            }

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

            var coverIndexOffset = MobiHeader.ExthHeader.CoverOffset;
            var thumbIndexOffset = MobiHeader.ExthHeader.ThumbOffset;

            PageRecords = new PageRecords(_azwStream, PdbHeader.Records, ImageType.SD,
                MobiHeader.FirstImageIndex, MobiHeader.LastContentRecordNumber,
                coverIndexOffset, thumbIndexOffset);

            await PageRecords.AnalyzePageRecordsAsync().ConfigureAwait(false);
        }

        public async Task ReadHDImageRecordsAsync(Stream hdContainerStream)
        {
            if (PdbHeader == null || MobiHeader == null)
            {
                throw new MobiMetadataException("Must call ReadMetadataAsync before calling ReadHDImageRecordsAsync");
            }

            if (MobiHeader.SkipProperties)
            {
                throw new MobiMetadataException($"Cannot read HD image records ({nameof(MobiHeader.SkipProperties)} is {MobiHeader.SkipProperties}).");
            }

            if (MobiHeader.SkipExthHeader)
            {
                throw new MobiMetadataException($"Cannot read HD image records ({nameof(MobiHeader.SkipExthHeader)} is {MobiHeader.SkipExthHeader}).");
            }

            var hdPdbHeader = new PDBHead();
            await hdPdbHeader.ReadHeaderAsync(hdContainerStream).ConfigureAwait(false);

            if (!hdPdbHeader.IsHDImageContainer)
            {
                throw new MobiMetadataException("Not a HD image container");
            }

            // The azw6 header is the first pdb record in the HD container 
            Azw6Header = new Azw6Head(skipExthHeader: true);
            await Azw6Header.ReadHeaderAsync(hdContainerStream).ConfigureAwait(false);

            if (Azw6Header.Title != MobiHeader.FullName) 
            {
                throw new MobiMetadataException(
                    $"{nameof(Azw6Header.Title)} / {nameof(MobiHeader.FullName)} mismatch: [{Azw6Header.Title}] vs [{MobiHeader.FullName}]");
            }

            var coverIndexOffset = MobiHeader.ExthHeader.CoverOffset;
            var thumbIndexOffset = MobiHeader.ExthHeader.ThumbOffset;

            PageRecordsHD = new PageRecords(hdContainerStream, hdPdbHeader.Records, ImageType.HD,
                1, (ushort)(hdPdbHeader.Records.Length - 1),
                coverIndexOffset, thumbIndexOffset);

            await PageRecordsHD.AnalyzePageRecordsHDAsync(PageRecords.ContentRecords.Count).ConfigureAwait(false);
        }
    }
}
