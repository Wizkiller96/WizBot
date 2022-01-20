namespace NadekoBot.Services.Currency;

public record class Extra(string Type, string Subtype, string Note = "", ulong OtherId = 0);