using System.Text.Json.Serialization;

namespace WizBot.Modules.Searches;

public sealed class NasdaqSummaryResponse
{
    public required string Symbol { get; init; }

    public required NasdaqSummaryResponseData SummaryData { get; init; }
    public required NasdaqSummaryBidAsk BidAsk { get; init; }

    public sealed class NasdaqSummaryBidAsk
    {
        [JsonPropertyName("Bid * Size")]
        public required NasdaqBid Bid { get; init; }

        public sealed class NasdaqBid
        {
            public required string Value { get; init; }
        }
    }

    public sealed class NasdaqSummaryResponseData
    {
        public required PreviousCloseData PreviousClose { get; init; }
        public required MarketCapData MarketCap { get; init; }
        public required AverageVolumeData AverageVolume { get; init; }

        public sealed class PreviousCloseData
        {
            public required string Value { get; init; }
        }

        public sealed class MarketCapData
        {
            public required string Value { get; init; }
        }

        public sealed class AverageVolumeData
        {
            public required string Value { get; init; }
        }
    }
}