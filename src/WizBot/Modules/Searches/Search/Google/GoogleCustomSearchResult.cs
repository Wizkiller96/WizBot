using WizBot.Modules.Searches;
using System.Text.Json.Serialization;

namespace WizBot.Services;

public sealed class GoogleCustomSearchResult : ISearchResult
{
    ISearchResultInformation ISearchResult.Info
        => Info;

    public string? Answer
        => null;

    IReadOnlyCollection<ISearchResultEntry> ISearchResult.Entries
        => Entries ?? Array.Empty<OfficialGoogleSearchResultEntry>();

    [JsonPropertyName("searchInformation")]
    public GoogleSearchResultInformation Info { get; init; } = null!;

    [JsonPropertyName("items")]
    public IReadOnlyCollection<OfficialGoogleSearchResultEntry>? Entries { get; init; }
}