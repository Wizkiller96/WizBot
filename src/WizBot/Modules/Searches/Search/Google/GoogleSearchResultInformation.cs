﻿using WizBot.Modules.Searches;
using System.Text.Json.Serialization;

namespace WizBot.Services;

public sealed class GoogleSearchResultInformation : ISearchResultInformation
{
    [JsonPropertyName("formattedTotalResults")]
    public string TotalResults { get; init; } = null!;

    [JsonPropertyName("formattedSearchTime")]
    public string SearchTime { get; init; } = null!;
}