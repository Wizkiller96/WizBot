namespace WizBot.Modules.Searches.Services;

public class WikipediaReply
{
    public class Info
    {
        public required string Url { get; init; }
    }

    public required Info Data { get; init; }
}