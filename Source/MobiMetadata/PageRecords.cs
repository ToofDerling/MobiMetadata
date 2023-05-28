namespace MobiMetadata
{
    public class PageRecords
    {
        private readonly List<PageRecord> _allRecords;

        public List<PageRecord> ImageRecords { get; set; }

        public List<PageRecord> RestOfRecords { get; set; }

        public ImageType ImageType { get; private set; }

        public PageRecord? CoverRecord { get; private set; }

        public PageRecord? ThumbImage { get; private set; }

        public PageRecord? DatpRecord { get; set; }

        public PageRecord? Len1992Record { get; set; }

        public RescRecord RescRecord { get; set; }

        public List<PageRecord> FontRecords { get; set; }

        // Special record for the HD image container.

        public PageRecord? KindleEmbedRecord { get; set; }

        public PageRecords(Stream stream, PDBRecordInfo[] pdbRecords, ImageType imageType, uint firstImageIndex, ushort lastImageIndex, 
            uint coverIndexOffset, uint thumbIndexOffset)
        {
            ImageType = imageType;

            CoverRecord = null;
            ThumbImage = null;

            var coverIndex = firstImageIndex + coverIndexOffset;
            var thumbIndex = firstImageIndex + thumbIndexOffset;

            _allRecords = new List<PageRecord>();
            for (var index = firstImageIndex; index < lastImageIndex; index++)
            {
                var pdbRecord = pdbRecords[index];

                var next = index + 1;
                var nextRecord = pdbRecords[next];

                var dataOffset = pdbRecord.RecordDataOffset;
                var nextRecordOffset = nextRecord.RecordDataOffset;

                PageRecord pageRecord;
                if (imageType == ImageType.SD)
                {
                    pageRecord = new PageRecord(stream, dataOffset, nextRecordOffset - dataOffset);
                }
                else
                {
                    pageRecord = new PageRecordHD(stream, dataOffset, nextRecordOffset - dataOffset);
                }

                if (coverIndexOffset > 0 && index == coverIndex)
                {
                    CoverRecord = pageRecord;
                }
                else if (thumbIndexOffset > 0 && index == thumbIndex)
                {
                    ThumbImage = pageRecord;
                }
                else
                {
                    _allRecords.Add(pageRecord);
                }
            }
        }

        public async Task AnalyzePageRecordsAsync()
        {
            // Search backwards for the RESC record 
            var rescIndex = _allRecords.Count - 1;
            for (; rescIndex >= 0; rescIndex--)
            {
                var record = _allRecords[rescIndex];

                var rescRecord = await record.GetRescRecordAsync().ConfigureAwait(false);
                if (rescRecord != null)
                {
                    await rescRecord.ParseXmlAsync().ConfigureAwait(false);
                    RescRecord = rescRecord;

                    break;
                }
            }
            if (RescRecord == null)
            {
                throw new MobiMetadataException($"Found no {nameof(RescRecord)}");
            }
            _allRecords.RemoveAt(rescIndex);

            // Pagecount fixup for some manga books
            if (_allRecords.Count == RescRecord.PageCount)
            {
                var lastRecord = _allRecords.Count - 1;
                if (await _allRecords[lastRecord].IsDatpRecordAsync().ConfigureAwait(false))
                {
                    _allRecords.RemoveAt(lastRecord);
                    RescRecord.AdjustPageCountBy(-1);
                }
            }

            // If we have more records than pagecount filter out the known types
            if (_allRecords.Count > RescRecord.PageCount)
            {
                var restOfRecords = _allRecords.Skip(RescRecord.PageCount).ToList();

                for (int i = 0, sz = restOfRecords.Count; i < sz; i++)
                {
                    var record = restOfRecords[i];

                    // The DATP record
                    if (await record.IsDatpRecordAsync().ConfigureAwait(false))
                    {
                        DatpRecord = record;
                        restOfRecords[i] = null;
                    }
                    // The 1992 bytes image
                    else if (record.IsLen1992Record())
                    {
                        Len1992Record = record;
                        restOfRecords[i] = null;
                    }
                }

                // Set the "real" rest and the content records
                RestOfRecords = restOfRecords.Where(record => record != null).ToList();
                ImageRecords = _allRecords.Take(RescRecord.PageCount).ToList();
            }
            else
            {
                ImageRecords = _allRecords;
            }

            if (ImageRecords.Count < RescRecord.PageCount)
            {
                throw new MobiMetadataException($"{nameof(ImageRecords)} {ImageRecords.Count} < {nameof(RescRecord.PageCount)} {RescRecord.PageCount}");
            }
        }

        public async Task AnalyzeHdContainerRecordsAsync(int pageCount)
        {
            if (_allRecords.Count > pageCount)
            {
                var restOfRecords = _allRecords.Skip(pageCount).ToList();

                for (int i = 0, sz = restOfRecords.Count; i < sz; i++)
                {
                    var record = restOfRecords[i];

                    // The kindle:embed record
                    if (await record.IsKindleEmbedRecordAsync().ConfigureAwait(false))
                    {
                        KindleEmbedRecord = record;
                        restOfRecords[i] = null;
                    }
                }

                // Set the "real" rest and the image records
                RestOfRecords = restOfRecords.Where(record => record != null).ToList();
                ImageRecords = _allRecords.Take(pageCount).ToList();
            }
            else
            {
                ImageRecords = _allRecords;
            }

            if (ImageRecords.Count != pageCount)
            {
                throw new MobiMetadataException($"HD container pageCount {ImageRecords.Count} vs SD pageCount {pageCount} mismatch?");
            }
        }
    }
}
