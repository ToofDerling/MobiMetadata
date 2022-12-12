# MobiMetadata

MobiMetadata is a .NET library you can use to read strongly typed metadata from .azw files (with no drm) and save all images, including HD images from the azw.res file, to a stream of your choice.  

MobiMetadata targets .NET 6.0, uses a lot of recent language features and is fully async. But the documentation is what you're reading here, the code is pretty much uncommented, and there's no Nuget package. Clone it, compile it, and check it out.

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
    && await hdImageRecords.CoverRecord.TryWriteHDImageDataAsync(yourStream))
{
   // Got a HD cover
}

// Save the SD cover if available
if (metadata.PageRecords.CoverRecord != null
{
    await sdImageRecords.CoverRecord.WriteDataAsync(yourStream);
}
</pre>

More to come...

**Credits.**

The azw parser is originally copied from the [Mobi Metadata Reader](https://www.mobileread.com/forums/showthread.php?t=185565) by Limey. I cleaned it up, modernized it, fixed the FullName parsing, and added support for retrieving SD and HD images, the rest is Limey's work. This [Stack Overflow post](https://stackoverflow.com/questions/24233834/getting-cover-image-from-a-mobi-file) was very helpful when figuring out how to extract cover images.
