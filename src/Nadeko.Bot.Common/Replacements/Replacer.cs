#nullable disable
using System.Text.RegularExpressions;

namespace NadekoBot.Common;

public class Replacer
{
    private readonly IEnumerable<(Regex Regex, Func<Match, string> Replacement)> _regex;
    private readonly IEnumerable<(string Key, Func<string> Text)> _replacements;

    public Replacer(IEnumerable<(string, Func<string>)> replacements, IEnumerable<(Regex, Func<Match, string>)> regex)
    {
        _replacements = replacements;
        _regex = regex;
    }

    public string Replace(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        foreach (var (key, text) in _replacements)
        {
            if (input.Contains(key))
                input = input.Replace(key, text(), StringComparison.InvariantCulture);
        }

        foreach (var item in _regex)
            input = item.Regex.Replace(input, m => item.Replacement(m));

        return input;
    }

    public SmartText Replace(SmartText data)
        => data switch
        {
            SmartEmbedText embedData => Replace(embedData) with
            {
                PlainText = Replace(embedData.PlainText),
                Color = embedData.Color
            },
            SmartPlainText plain => Replace(plain),
            SmartEmbedTextArray arr => Replace(arr), 
            _ => throw new ArgumentOutOfRangeException(nameof(data), "Unsupported argument type")
        };

    private SmartEmbedTextArray Replace(SmartEmbedTextArray embedArr)
        => new()
        {
            Embeds = embedArr.Embeds.Map(e => Replace(e) with
            {
                Color = e.Color
            }),
            Content = Replace(embedArr.Content)
        };

    private SmartPlainText Replace(SmartPlainText plain)
        => Replace(plain.Text);

    private T Replace<T>(T embedData) where T: SmartEmbedTextBase, new()
    {
        var newEmbedData = new T
        {
            Description = Replace(embedData.Description),
            Title = Replace(embedData.Title),
            Thumbnail = Replace(embedData.Thumbnail),
            Image = Replace(embedData.Image),
            Url = Replace(embedData.Url),
            Author = embedData.Author is null
                ? null
                : new()
                {
                    Name = Replace(embedData.Author.Name),
                    IconUrl = Replace(embedData.Author.IconUrl)
                },
            Fields = embedData.Fields?.Map(f => new SmartTextEmbedField
            {
                Name = Replace(f.Name),
                Value = Replace(f.Value),
                Inline = f.Inline
            }),
            Footer = embedData.Footer is null
                ? null
                : new()
                {
                    Text = Replace(embedData.Footer.Text),
                    IconUrl = Replace(embedData.Footer.IconUrl)
                }
        };

        return newEmbedData;
    }
}