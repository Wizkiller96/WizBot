#nullable disable
using System.Text.Json;

namespace WizBot;

public abstract record SmartText
{
    public bool IsEmbed
        => this is SmartEmbedText;

    public bool IsPlainText
        => this is SmartPlainText;

    public bool IsEmbedArray
        => this is SmartEmbedTextArray;

    private static readonly JsonSerializerOptions _opts = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static SmartText operator +(SmartText text, string input)
        => text switch
        {
            SmartEmbedText set => set with
            {
                PlainText = set.PlainText + input
            },
            SmartPlainText spt => new SmartPlainText(spt.Text + input),
            SmartEmbedTextArray arr => arr with
            {
                PlainText = arr.PlainText + input
            },
            _ => throw new ArgumentOutOfRangeException(nameof(text))
        };

    public static SmartText operator +(string input, SmartText text)
        => text switch
        {
            SmartEmbedText set => set with
            {
                PlainText = input + set.PlainText
            },
            SmartPlainText spt => new SmartPlainText(input + spt.Text),
            SmartEmbedTextArray arr => arr with
            {
                PlainText = input + arr.PlainText
            },
            _ => throw new ArgumentOutOfRangeException(nameof(text))
        };

    public static SmartText CreateFrom(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new SmartPlainText(input);

        try
        {
            var doc = JsonDocument.Parse(input);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("embeds", out _))
                {
                    var arr = root.Deserialize<SmartEmbedTextArray>(_opts);

                    if (arr is null)
                        return new SmartPlainText(input);

                    arr!.NormalizeFields();
                    return arr;
                }

                var obj = root.Deserialize<SmartEmbedText>(_opts);

                if (obj is null)
                    return new SmartPlainText(input);

                obj.NormalizeFields();
                return obj;
            }
            
            return new SmartPlainText(input);
        }
        catch
        {
            return new SmartPlainText(input);
        }
    }
}