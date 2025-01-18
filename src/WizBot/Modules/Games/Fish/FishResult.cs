namespace WizBot.Modules.Games;

public sealed class FishResult
{
    public required FishData Fish { get; init; }
    public int Stars { get; init; }
}

public readonly record struct AlreadyFishing;