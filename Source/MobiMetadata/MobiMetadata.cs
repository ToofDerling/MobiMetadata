using AzwConverter;
using System.IO;

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

        public MobiMetadata(Stream stream, PDBHead pdbHeader = null, PalmDOCHead palmDocHeader = null, MobiHead mobiHeader = null,
            EXTHHead exthHeader = null, bool throwIfNoExthHeader = false)
        {
            _pdbHeader = pdbHeader ?? MobiHeaderFactory.CreateReadAll<PDBHead>();
            _pdbHeader.ReadHeader(stream);

            _palmDocHeader = palmDocHeader ?? MobiHeaderFactory.CreateReadAll<PalmDOCHead>();
            _palmDocHeader.ReadHeader(stream);

            _mobiHeader = mobiHeader ?? MobiHeaderFactory.CreateReadAll<MobiHead>();

            _mobiHeader.PreviousHeaderPosition = _palmDocHeader.Position;
            _mobiHeader.SetExthHeader(exthHeader);

            // This also reads the exthheader
            _mobiHeader.ReadHeader(stream);

            if (_mobiHeader.ExthHeader == null)
            {
                if (throwIfNoExthHeader)
                {
                    throw new MobiMetadataException($"{mobiHeader.FullName}: No EXTHHeader");
                }
            }
        }

        public async Task ReadImageRecordsAsync(Stream stream)
        {
            if (PdbHeader.RecordInfoIsEmpty)
            {
                throw new MobiMetadataException("Cannot read image records: record information is empty"); 
            }

            var coverIndexOffset = _mobiHeader.ExthHeader.CoverOffset;
            var thumbIndexOffset = _mobiHeader.ExthHeader.ThumbOffset;

            PageRecords = new PageRecords(stream, _pdbHeader.Records, ImageType.SD,
                _mobiHeader.FirstImageIndex, _mobiHeader.LastContentRecordNumber,
                coverIndexOffset, thumbIndexOffset);

            await PageRecords.AnalyzePageRecordsAsync();

        }
  
        public async Task ReadHDImageRecordsAsync(Stream hdContainerStream)
        {
            var pdbHeader = MobiHeaderFactory.CreateReadAll<PDBHead>();
            MobiHeaderFactory.ConfigureRead(pdbHeader, pdbHeader.TypeAttr, pdbHeader.CreatorAttr, 
                pdbHeader.NumRecordsAttr);
            
            pdbHeader.ReadHeader(hdContainerStream);

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
