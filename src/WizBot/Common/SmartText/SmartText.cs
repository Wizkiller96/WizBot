﻿#nullable disable
using Newtonsoft.Json;

namespace WizBot;

public abstract record SmartText
{
    public bool IsEmbed
        => this is SmartEmbedText;

    public bool IsPlainText
        => this is SmartPlainText;

    public static SmartText operator +(SmartText text, string input)
        => text switch
        {
            SmartEmbedText set => set with
            {
                PlainText = set.PlainText + input
            },
            SmartPlainText spt => new SmartPlainText(spt.Text + input),
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
            _ => throw new ArgumentOutOfRangeException(nameof(text))
        };

    public static SmartText CreateFrom(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || !input.TrimStart().StartsWith("{"))
            return new SmartPlainText(input);

        try
        {
            var smartEmbedText = JsonConvert.DeserializeObject<SmartEmbedText>(input);

            if (smartEmbedText is null)
                throw new FormatException();

            smartEmbedText.NormalizeFields();

            if (!smartEmbedText.IsValid)
                return new SmartPlainText(input);

            return smartEmbedText;
        }
        catch
        {
            return new SmartPlainText(input);
        }
    }
}