using System.Text.Json;
using WizBot.Core.Common.JsonConverters;

namespace WizBot.Core.Common
{
    public class JsonSeria : ISeria
    {
        private JsonSerializerOptions serializerOptions = new JsonSerializerOptions()
        {
            Converters =
            {
                new Rgba32Converter(),
                new CultureInfoConverter(),
            }
        };
        public byte[] Serialize<T>(T data)
            => JsonSerializer.SerializeToUtf8Bytes(data, serializerOptions);

        public T Deserialize<T>(byte[] data)
        {
            if (data is null)
                return default;


            return JsonSerializer.Deserialize<T>(data, serializerOptions);
        }
    }
}