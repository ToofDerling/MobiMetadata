using MobiMetadata;

namespace AzwConverter
{
    public class MobiHeaderFactory
    {
        /// <summary>
        /// Do not read any records of this header. Shortcut for CreateReadAll -> ConfigureRead(header, null).
        /// Calling ConfigureRead on a header created by this method throws a MobiMetaDataException. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="MobiMetadataException"></exception>
        /// <returns></returns>
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
            if (!header.IsReadAll)
            {
                throw new MobiMetadataException("You can only ConfigureRead a header created by CreateReadAll.");
            }
            header.SetAttrsToRead(attrsToRead);
        }
    }
}
