using System.Text.Json;

namespace WizBot.Core.Common
{
    public class JsonSeria : ISeria
    {
        public byte[] Serialize<T>(T data)
            => JsonSerializer.SerializeToUtf8Bytes(data);

        public T Deserialize<T>(byte[] data)
        {
            if (data is null)
                return default;


            return JsonSerializer.Deserialize<T>(data);
        }
    }
}