namespace NadekoBot.Modules.Searches.GoogleScrape;

public class PlainGoogleScrapeSearchResult : ISearchResult
{
    public string? Answer { get; init;  } = null!;
    public IReadOnlyCollection<ISearchResultEntry> Entries { get; init; } = null!;
    public ISearchResultInformation Info { get; init; } = null!;
}