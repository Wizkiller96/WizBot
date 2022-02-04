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
            SmartEmbedText embedData => Replace(embedData),
            SmartPlainText plain => Replace(plain),
            _ => throw new ArgumentOutOfRangeException(nameof(data), "Unsupported argument type")
        };

    public SmartPlainText Replace(SmartPlainText plainText)
        => Replace(plainText.Text);

    public SmartEmbedText Replace(SmartEmbedText embedData)
    {
        var newEmbedData = new SmartEmbedText
        {
            PlainText = Replace(embedData.PlainText),
            Description = Replace(embedData.Description),
            Title = Replace(embedData.Title),
            Thumbnail = Replace(embedData.Thumbnail),
            Image = Replace(embedData.Image),
            Url = Replace(embedData.Url)
        };
        if (embedData.Author is not null)
        {
            newEmbedData.Author = new()
            {
                Name = Replace(embedData.Author.Name),
                IconUrl = Replace(embedData.Author.IconUrl)
            };
        }

        if (embedData.Fields is not null)
        {
            var fields = new List<SmartTextEmbedField>();
            foreach (var f in embedData.Fields)
            {
                var newF = new SmartTextEmbedField
                {
                    Name = Replace(f.Name),
                    Value = Replace(f.Value),
                    Inline = f.Inline
                };
                fields.Add(newF);
            }

            newEmbedData.Fields = fields.ToArray();
        }

        if (embedData.Footer is not null)
        {
            newEmbedData.Footer = new()
            {
                Text = Replace(embedData.Footer.Text),
                IconUrl = Replace(embedData.Footer.IconUrl)
            };
        }

        newEmbedData.Color = embedData.Color;

        return newEmbedData;
    }
}