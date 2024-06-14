namespace WizBot.Modules.Utility;

public sealed class WizBotCommandCallModel
{
    public required string Name { get; set; }
    public required IReadOnlyList<string> Arguments { get; set; }
    public required string Remaining { get; set; }
}