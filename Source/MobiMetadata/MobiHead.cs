using System.Text;

namespace MobiMetadata
{
    public class MobiHead : BaseHeader
    {
        public readonly Attr IdentifierAttr = new(4);

        public readonly Attr HeaderLengthAttr = new(4);

        public readonly Attr MobiTypeAttr = new(4);

        public readonly Attr TextEncodingAttr = new(4);

        public readonly Attr UniqueIDAttr = new(4);

        public readonly Attr FileVersionAttr = new(4);

        public readonly Attr OrthographicIndexAttr = new(4);

        public readonly Attr InflectionIndexAttr = new(4);

        public readonly Attr IndexNamesAttr = new(4);

        public readonly Attr IndexKeysAttr = new(4);

        public readonly Attr ExtraIndex0Attr = new(4);

        public readonly Attr ExtraIndex1Attr = new(4);

        public readonly Attr ExtraIndex2Attr = new(4);

        public readonly Attr ExtraIndex3Attr = new(4);

        public readonly Attr ExtraIndex4Attr = new(4);

        public readonly Attr ExtraIndex5Attr = new(4);

        public readonly Attr FirstNonBookIndexAttr = new(4);

        public readonly Attr FullNameOffsetAttr = new(4);

        public readonly Attr FullNameLengthAttr = new(4);

        public readonly Attr LocaleAttr = new(4);

        public readonly Attr InputLanguageAttr = new(4);

        public readonly Attr OutputLanguageAttr = new(4);

        public readonly Attr MinVersionAttr = new(4);

        public readonly Attr FirstImageIndexAttr = new(4);

        public readonly Attr HuffmanRecordOffsetAttr = new(4);

        public readonly Attr HuffmanRecordCountAttr = new(4);

        public readonly Attr HuffmanTableOffsetAttr = new(4);

        public readonly Attr HuffmanTableLengthAttr = new(4);

        public readonly Attr ExthFlagsAttr = new(4);

        //132	0x84	32	?	32 unknown bytes, if MOBI is long enough
        public readonly Attr Unknown1Attr = new(32);

        //164	0xa4	4	Unknown Use 0xFFFFFFFF
        public readonly Attr Unknown2Attr = new(4);

        //168	0xa8	4	DRM Offset  Offset to DRM key info in DRMed files. 0xFFFFFFFF if no DRM
        public readonly Attr DrmOffsetAttr = new(4);

        //172	0xac	4	DRM Count   Number of entries in DRM info. 0xFFFFFFFF if no DRM
        public readonly Attr DrmCountAttr = new(4);

        //176	0xb0	4	DRM Size    Number of bytes in DRM info.
        public readonly Attr DrmSizeAttr = new(4);

        //180	0xb4	4	DRM Flags   Some flags concerning the DRM info.
        public readonly Attr DrmFlagsAttr = new(4);

        //184	0xb8	8	Unknown Bytes to the end of the MOBI header, including the following if the header length >= 228 (244 from start of record). Use 0x0000000000000000.
        public readonly Attr Unknown3Attr = new(8);

        //192	0xc0	2	First content record number Number of first text record. Normally 1.
        public readonly Attr FirstContentRecordNumberAttr = new(2);

        //194	0xc2	2	Last content record number  Number of last image record or number of last text record if it contains no images.Includes Image, DATP, HUFF, DRM.
        public readonly Attr LastContentRecordNumberAttr = new(2);

        private EXTHHead exthHeader = null;

        private Attr fullName;

        internal long PreviousHeaderPosition { get; set; }

        public override void ReadHeader(Stream stream)
        {
            var mobiHeaderOffset = stream.Position;

            Read(stream, IdentifierAttr);
            if (IdentifierAsString != "MOBI")
            {
                throw new MobiMetadataException("Did not get expected MOBI identifier");
            }

            // Need the header length to read the EXTH header
            var readExthHeader = IsAttrToRead(ExthFlagsAttr);
            if (readExthHeader)
            {
                Read(stream, HeaderLengthAttr);
            }
            else
            {
                ReadOrSkip(stream, HeaderLengthAttr);
            }

            ReadOrSkip(stream, MobiTypeAttr);
            ReadOrSkip(stream, TextEncodingAttr);
            ReadOrSkip(stream, UniqueIDAttr);
            ReadOrSkip(stream, FileVersionAttr);
            ReadOrSkip(stream, OrthographicIndexAttr);
            ReadOrSkip(stream, InflectionIndexAttr);
            ReadOrSkip(stream, IndexNamesAttr);
            ReadOrSkip(stream, IndexKeysAttr);
            ReadOrSkip(stream, ExtraIndex0Attr);
            ReadOrSkip(stream, ExtraIndex1Attr);
            ReadOrSkip(stream, ExtraIndex2Attr);
            ReadOrSkip(stream, ExtraIndex3Attr);
            ReadOrSkip(stream, ExtraIndex4Attr);
            ReadOrSkip(stream, ExtraIndex5Attr);
            ReadOrSkip(stream, FirstNonBookIndexAttr);

            // Handle these together as both are needed to read the fullname
            var readFullName = IsAttrToRead(FullNameOffsetAttr) || IsAttrToRead(FullNameLengthAttr);
            if (readFullName)
            {
                Read(stream, FullNameOffsetAttr);
                Read(stream, FullNameLengthAttr);
            }
            else
            {
                Skip(stream, FullNameOffsetAttr);
                Skip(stream, FullNameLengthAttr);
            }

            ReadOrSkip(stream, LocaleAttr);
            ReadOrSkip(stream, InputLanguageAttr);
            ReadOrSkip(stream, OutputLanguageAttr);
            ReadOrSkip(stream, MinVersionAttr);
            ReadOrSkip(stream, FirstImageIndexAttr);
            ReadOrSkip(stream, HuffmanRecordOffsetAttr);
            ReadOrSkip(stream, HuffmanRecordCountAttr);
            ReadOrSkip(stream, HuffmanTableOffsetAttr);
            ReadOrSkip(stream, HuffmanTableLengthAttr);
            ReadOrSkip(stream, ExthFlagsAttr);
            Skip(stream, Unknown1Attr);
            Skip(stream, Unknown2Attr);
            Skip(stream, DrmOffsetAttr);
            Skip(stream, DrmCountAttr);
            Skip(stream, DrmSizeAttr);
            Skip(stream, DrmFlagsAttr);
            Skip(stream, Unknown3Attr);
            ReadOrSkip(stream, FirstContentRecordNumberAttr);
            ReadOrSkip(stream, LastContentRecordNumberAttr);

            if (readExthHeader)
            {
                ReadExthHeader(stream, mobiHeaderOffset);
            }

            if (readFullName) 
            {
                ReadFullName(stream);
            }
        }

        private void ReadFullName(Stream stream)
        {
            //Read the fullname
            var fullnamePos = PreviousHeaderPosition + FullNameOffset;
            stream.Position = fullnamePos;

            fullName = new Attr((int)FullNameLength);
            Read(stream, fullName);
        }

        private void ReadExthHeader(Stream stream, long mobiHeaderOffset)
        {
            //If bit 6 (0x40) is set, then there's an EXTH record 
            bool exthExists = (Converter.ToUInt32(ExthFlagsAttr.Data) & 0x40) != 0;
            if (exthExists)
            {
                // The EXTH header immediately follows the EXTH header, but as the MOBI header is of
                // variable length, we have to calculate the EXTH header offset.
                var exthOffset = mobiHeaderOffset + HeaderLength;
                stream.Position = exthOffset;

                exthHeader = new EXTHHead(Array.Empty<Attr>());
                exthHeader.ReadHeader(stream);
            }
            else
            {
                exthHeader = new EXTHHead();
            }
        }

        //Properties
        public int ExthHeaderSize => exthHeader.Size;

        public string FullName => Encoding.UTF8.GetString(fullName.Data);

        public string IdentifierAsString => Encoding.UTF8.GetString(IdentifierAttr.Data).Replace("\0", string.Empty);

        public uint HeaderLength => Converter.ToUInt32(HeaderLengthAttr.Data);

        public uint FirstImageIndex => Converter.ToUInt32(FirstImageIndexAttr.Data);

        public uint MobiType => Converter.ToUInt32(MobiTypeAttr.Data);

        public string MobiTypeAsString => MobiType switch
        {
            2 => "Mobipocket Book",
            3 => "PalmDoc Book",
            4 => "Audio",
            257 => "News",
            258 => "News Feed",
            259 => "News Magazine",
            513 => "PICS",
            514 => "WORD",
            515 => "XLS",
            516 => "PPT",
            517 => "TEXT",
            518 => "HTML",
            _ => $"Unknown (0)",
        };

        public uint TextEncoding => Converter.ToUInt32(TextEncodingAttr.Data);

        public string TextEncodingAsString => TextEncoding switch
        {
            1252 => "Cp1252",
            65001 => "UTF-8",
            _ => null,
        };

        public uint UniqueID => Converter.ToUInt32(UniqueIDAttr.Data);

        public uint FileVersion => Converter.ToUInt32(FileVersionAttr.Data);

        public uint OrthographicIndex => Converter.ToUInt32(OrthographicIndexAttr.Data);

        public uint InflectionIndex => Converter.ToUInt32(InflectionIndexAttr.Data);

        public uint IndexNames => Converter.ToUInt32(IndexNamesAttr.Data);

        public uint IndexKeys => Converter.ToUInt32(IndexKeysAttr.Data);

        public uint ExtraIndex0 => Converter.ToUInt32(ExtraIndex0Attr.Data);

        public uint ExtraIndex1 => Converter.ToUInt32(ExtraIndex1Attr.Data);

        public uint ExtraIndex2 => Converter.ToUInt32(ExtraIndex2Attr.Data);

        public uint ExtraIndex3 => Converter.ToUInt32(ExtraIndex3Attr.Data);

        public uint ExtraIndex4 => Converter.ToUInt32(ExtraIndex4Attr.Data);

        public uint ExtraIndex5 => Converter.ToUInt32(ExtraIndex5Attr.Data);

        public uint FirstNonBookIndex => Converter.ToUInt32(FirstNonBookIndexAttr.Data);

        public uint FullNameOffset => Converter.ToUInt32(FullNameOffsetAttr.Data);

        public uint FullNameLength => Converter.ToUInt32(FullNameLengthAttr.Data);

        public uint MinVersion => Converter.ToUInt32(MinVersionAttr.Data);

        public uint HuffmanRecordOffset => Converter.ToUInt32(HuffmanRecordOffsetAttr.Data);

        public uint HuffmanRecordCount => Converter.ToUInt32(HuffmanRecordCountAttr.Data);

        public uint HuffmanTableOffset => Converter.ToUInt32(HuffmanTableOffsetAttr.Data);

        public uint HuffmanTableLength => Converter.ToUInt32(HuffmanTableLengthAttr.Data);

        public ushort FirstContentRecordNumber => Converter.ToUInt16(FirstContentRecordNumberAttr.Data);

        public ushort LastContentRecordNumber => Converter.ToUInt16(LastContentRecordNumberAttr.Data);

        public EXTHHead EXTHHeader => exthHeader;
    }
}
