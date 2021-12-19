using System.Text.Json;

namespace SystemTextJsonSamples
{
    public class LowerCaseNamingPolicy : JsonNamingPolicy
    {
        public static LowerCaseNamingPolicy Default = new LowerCaseNamingPolicy();
        
        public override string ConvertName(string name) =>
            name.ToLower();
    }
}