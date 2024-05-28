#nullable disable

using System.Text.Json.Serialization;

namespace WizBot.Modules.Searches.Common;

public class BibleVerses
{
    public string Error { get; set; }
    public BibleVerse[] Verses { get; set; }
}

public class BibleVerse
{
    [JsonPropertyName("book_name")]
    public string BookName { get; set; }

    public int Chapter { get; set; }
    public int Verse { get; set; }
    public string Text { get; set; }
}