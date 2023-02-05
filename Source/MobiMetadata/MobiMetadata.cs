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

        public PageRecords? HdContainerRecords { get; private set; }

        public List<PageRecord> MergedImageRecords { get; private set; }

        public PageRecord MergedCoverRecord { get; private set; }

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

        private void SetPageRecords()
        {
            if (PdbHeader == null || MobiHeader == null)
            {
                throw new MobiMetadataException($"Must call {nameof(ReadMetadataAsync)} before calling {nameof(SetPageRecords)}");
            }

            if (PdbHeader.SkipRecords)
            {
                throw new MobiMetadataException($"Cannot set page records ({nameof(PdbHeader.SkipRecords)} is {PdbHeader.SkipRecords}).");
            }

            if (MobiHeader.SkipProperties)
            {
                throw new MobiMetadataException($"Cannot set page records ({nameof(MobiHeader.SkipProperties)} is {MobiHeader.SkipProperties}).");
            }

            if (MobiHeader.SkipExthHeader)
            {
                throw new MobiMetadataException($"Cannot set page records ({nameof(MobiHeader.SkipExthHeader)} is {MobiHeader.SkipExthHeader}).");
            }

            var coverIndexOffset = MobiHeader.ExthHeader.CoverOffset;
            var thumbIndexOffset = MobiHeader.ExthHeader.ThumbOffset;

            PageRecords = new PageRecords(_azwStream, PdbHeader.Records, ImageType.SD,
                MobiHeader.FirstImageIndex, MobiHeader.LastContentRecordNumber,
                coverIndexOffset, thumbIndexOffset);
        }

        private async Task SetHdContainerRecordsAsync(Stream hdContainerStream)
        {
            if (PdbHeader == null || MobiHeader == null)
            {
                throw new MobiMetadataException($"Must call {nameof(ReadMetadataAsync)} before calling {nameof(SetHdContainerRecordsAsync)}");
            }

            if (MobiHeader.SkipProperties)
            {
                throw new MobiMetadataException($"Cannot read HD container records ({nameof(MobiHeader.SkipProperties)} is {MobiHeader.SkipProperties}).");
            }

            if (MobiHeader.SkipExthHeader)
            {
                throw new MobiMetadataException($"Cannot read HD container records ({nameof(MobiHeader.SkipExthHeader)} is {MobiHeader.SkipExthHeader}).");
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

            HdContainerRecords = new PageRecords(hdContainerStream, hdPdbHeader.Records, ImageType.HD,
                1, (ushort)(hdPdbHeader.Records.Length - 1),
                coverIndexOffset, thumbIndexOffset);
        }

        public async Task SetImageRecordsAsync(Stream? hdContainerStream)
        {
            SetPageRecords();
            await PageRecords.AnalyzePageRecordsAsync().ConfigureAwait(false);

            if (hdContainerStream != null)
            {
                await SetHdContainerRecordsAsync(hdContainerStream);
                await HdContainerRecords!.AnalyzeHdContainerRecordsAsync(PageRecords.ImageRecords.Count).ConfigureAwait(false);
            }

            if (MobiHeader.ExthHeader.BookType != "comic")
            {
                await HandleFontRecordsForNonComicBookTypesAsync();
            }

            // Merge cover
            if (HdContainerRecords != null && HdContainerRecords.CoverRecord != null
                && !HdContainerRecords.CoverRecord.IsCresPlaceHolder())
            {
                MergedCoverRecord = HdContainerRecords.CoverRecord;
            }
            else if (PageRecords.CoverRecord != null)
            {
                MergedCoverRecord = PageRecords.CoverRecord;
            }

            // Merge image
            var mergedImageRecords = new List<PageRecord>();

            for (int i = 0, sz = PageRecords.ImageRecords.Count; i < sz; i++)
            {
                if (HdContainerRecords != null && !HdContainerRecords.ImageRecords[i].IsCresPlaceHolder())
                {
                    mergedImageRecords.Add(HdContainerRecords.ImageRecords[i]);
                }
                else
                {
                    mergedImageRecords.Add(PageRecords.ImageRecords[i]);
                }
            }

            MergedImageRecords = mergedImageRecords;

            if (MergedImageRecords.Count != PageRecords.RescRecord.PageCount)
            {
                throw new MobiMetadataException($"Merged images count {MergedImageRecords.Count} vs pageCount {PageRecords.RescRecord.PageCount} mismatch?");
            }
        }

        public bool IsHdCover()
        {
            return HdContainerRecords != null && HdContainerRecords.CoverRecord != null;
        }

        public bool IsSdCover()
        {
            return PageRecords != null && PageRecords.CoverRecord != null;
        }

        public bool IsHdPage(int pageIndex)
        { 
            return HdContainerRecords != null && !HdContainerRecords.ImageRecords[pageIndex].IsCresPlaceHolder();
        }

        private async Task HandleFontRecordsForNonComicBookTypesAsync()
        {
            var fontRecords = new List<PageRecord>();

            for (int i = 0, sz = PageRecords.ImageRecords.Count; i < sz; i++)
            {
                if (await PageRecords.ImageRecords[i].IsFontRecordAsync())
                {
                    fontRecords.Add(PageRecords.ImageRecords[i]);
                    PageRecords.ImageRecords[i] = null;

                    if (HdContainerRecords != null)
                    {
                        HdContainerRecords.ImageRecords[i] = null;
                    }

                    PageRecords.RescRecord.AdjustPageCountBy(-1);
                }
            }

            if (fontRecords.Count > 0)
            {
                PageRecords.FontRecords = fontRecords;

                PageRecords.ImageRecords = PageRecords.ImageRecords.Where(record => record != null).ToList();

                if (HdContainerRecords != null)
                {
                    HdContainerRecords.ImageRecords = HdContainerRecords.ImageRecords.Where(record => record != null).ToList();
                }
            }
        }
    }
}
