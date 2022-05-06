﻿#nullable disable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WizBot;

public abstract record SmartText
{
    public bool IsEmbed
        => this is SmartEmbedText;

    public bool IsPlainText
        => this is SmartPlainText;

    public bool IsEmbedArray
        => this is SmartEmbedTextArray;

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
                Content = arr.Content + input
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
                Content = input + arr.Content
            },
            _ => throw new ArgumentOutOfRangeException(nameof(text))
        };

    public static SmartText CreateFrom(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new SmartPlainText(input);

        try
        {
            var doc = JObject.Parse(input);
            var root = doc.Root;
            if (root.Type == JTokenType.Object)
            {
                if (((JObject)root).TryGetValue("embeds", out _))
                {
                    var arr = root.ToObject<SmartEmbedTextArray>();

                    if (arr is null)
                        return new SmartPlainText(input);

                    arr!.NormalizeFields();
                    return arr;
                }

                var obj = root.ToObject<SmartEmbedText>();

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