# MobiMetadata

MobiMetadata is a .NET library you can use to read strongly typed metadata from .azw files and save all images (including HD images from the azw.res file) to a stream of your choice.  

MobiMetadata targets .NET 6.0, it uses a lot of recent language features and is fully async.

**How to use.**

<pre>
var stream = File.Open(azwFile);
var metadata = MobiMetadata();
await metadata.ReadMetadataAsync(stream);
</pre>

**Credits.**

The azw parser is originally copied from the [Mobi Metadata Reader](https://www.mobileread.com/forums/showthread.php?t=185565) by Limey. I cleaned it up, modernized it, fixed the FullName parsing, and added support for retrieving SD and HD images, the rest is Limey's work. This [Stack Overflow post](https://stackoverflow.com/questions/24233834/getting-cover-image-from-a-mobi-file) was very helpful when figuring out how to extract cover images.
