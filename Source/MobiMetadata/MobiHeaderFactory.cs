using MobiMetadata;

namespace AzwConverter
{
    public class MobiHeaderFactory
    {
        public static T CreateReadNone<T>() where T : BaseHead, new()
        {
            var header = CreateReadAll<T>();

            header.SetAttrsToRead(null);

            return header;
        }

        public static T CreateReadAll<T>() where T : BaseHead, new()
        {
            return new T();
        }

        public static void ConfigureRead<T>(T header, params BaseHead.Attr[] attrsToRead) where T : BaseHead, new()
        {
            header.SetAttrsToRead(attrsToRead);
        }
    }
}
