#nullable disable
using System.Text.Json;

namespace SystemTextJsonSamples;

public class LowerCaseNamingPolicy : JsonNamingPolicy
{
    public static LowerCaseNamingPolicy Default = new();

    public override string ConvertName(string name)
        => name.ToLower();
}