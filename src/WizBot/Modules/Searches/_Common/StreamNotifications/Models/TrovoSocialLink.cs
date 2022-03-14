﻿#nullable disable
using System.Text.Json.Serialization;

namespace WizBot.Modules.Searches.Common.StreamNotifications.Providers;

public class TrovoSocialLink
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}