#nullable disable
using System.Text.Json;

namespace WizBot.Modules.Searches.Common;

public class LowerCaseNamingPolicy : JsonNamingPolicy
{
    public static LowerCaseNamingPolicy Default = new();

    public override string ConvertName(string name)
        => name.ToLower();
}