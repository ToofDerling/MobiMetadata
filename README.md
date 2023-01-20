# MobiMetadata

MobiMetadata is a .NET library you can use to read strongly typed metadata from mobi/azw files (with no drm) and save all images - including HD images from the azw.res/azw6 file - to a stream of your choice.  

MobiMetadata targets .NET 6.0, is fully async, and uses a lot of recent language features. But the documentation is what you're reading here, the code is pretty much uncommented, and there's no Nuget package. Clone it, compile it, and hack away.

**How to use.**

<pre>
var stream = File.Open(azwFile);
var metadata = new MobiMetadata();

// This will read all properties and records (except for image records) in all headers 
await metadata.ReadMetadataAsync(stream);

var title = metadata.MobiHeader.FullName;
var updatedTitle = metadata.MobiHeader.ExthHeader.UpdatedTitle;
var publisher = metadata.MobiHeader.ExthHeader.Publisher;

// Read the SD images in the azw file
await metadata.ReadImageRecordsAsync();

// Read the HD images in the azw.res file
var hdStream = File.Open(azwResFile);
await metadata.ReadHDImageRecordsAsync(hdStream);

// Save the HD cover if available
if (metadata.PageRecordsHD != null && metadata.PageRecordsHD.CoverRecord != null 
    && await metadata.PageRecordsHD.CoverRecord.TryWriteHDImageDataAsync(yourStream))
{
   // Got a HD cover
}

// Save the SD cover if available
if (metadata.PageRecords.CoverRecord != null)
{
    await metadata.PageRecords.CoverRecord.WriteDataAsync(yourStream);
}

// Loop through pages
for (int i = 0; i < = metadata.PageRecords.ContentRecords.Count; i++)
{
    // Try to save the hd image
    if (metadata.PageRecordsHD == null 
        || !await hdImageRecords.ContentRecords[i].TryWriteHDImageDataAsync(yourStream))
    {
        // Fall back on the sd image 
        await metadata.PageRecords.ContentRecords[i].WriteDataAsync(yourStream);
    }
}
</pre>

Have a look at the [CbzMage source code](https://github.com/ToofDerling/CbzMage/tree/main/Source/AzwConverter) (the Engine classes and MetadataManager) for more examples. Here is a [page](https://wiki.mobileread.com/wiki/MOBI) with lots of information about the various headers and properties.

If you have any questions use [Discussions](https://github.com/ToofDerling/MobiMetadata/discussions). If you want to report a bug use [Issues](https://github.com/ToofDerling/MobiMetadata/issues).

**Credits.**

The parser is originally copied from the [Mobi Metadata Reader](https://www.mobileread.com/forums/showthread.php?t=185565) by Limey. I cleaned it up, modernized it, fixed the FullName parsing, and added support for retrieving SD and HD images, the rest is Limey's work. This [Stack Overflow post](https://stackoverflow.com/questions/24233834/getting-cover-image-from-a-mobi-file) was very helpful when figuring out how to extract cover images.
