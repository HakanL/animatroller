using Newtonsoft.Json;
using System;
using System.IO;

namespace Animatroller.AdminMessage
{
    public static class Serializer
    {
        public static void Serialize(object value, Stream s)
        {
            var serializer = new JsonSerializer();

            using (var writer = new StreamWriter(s, System.Text.Encoding.UTF8, 1024, true))
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                serializer.Serialize(jsonWriter, value);
                jsonWriter.Flush();
            }
        }

        public static object DeserializeFromStream(Stream stream, Type type)
        {
            var serializer = new JsonSerializer();

            using (var sr = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(sr))
            {
                return serializer.Deserialize(jsonTextReader, type);
            }
        }

        public static T DeserializeFromStream<T>(Stream stream)
        {
            return (T)DeserializeFromStream(stream, typeof(T));
        }
    }
}
