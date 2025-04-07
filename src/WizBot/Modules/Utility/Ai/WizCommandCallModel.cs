namespace WizBot.Modules.Utility;

public sealed class WizCommandCallModel
{
    public required string Name { get; set; }
    public required IReadOnlyList<string> Arguments { get; set; }
    public required string Remaining { get; set; }
}