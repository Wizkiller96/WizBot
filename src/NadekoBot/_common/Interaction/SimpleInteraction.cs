namespace NadekoBot;

public static class InteractionHelpers
{
    public static readonly IEmote ArrowLeft = Emote.Parse("<:x:1232256519844790302>");
    public static readonly IEmote ArrowRight = Emote.Parse("<:x:1232256515298295838>");
}

public abstract class SimpleInteractionBase
{
    public abstract Task TriggerAsync(SocketMessageComponent smc);
    public abstract ButtonBuilder Button { get; }
}

public class SimpleInteraction<T> : SimpleInteractionBase
{
    public override ButtonBuilder Button { get; }
    private readonly Func<SocketMessageComponent, T, Task> _onClick;
    private readonly T? _state;

    public SimpleInteraction(ButtonBuilder button, Func<SocketMessageComponent, T?, Task> onClick, T? state = default)
    {
        Button = button;
        _onClick = onClick;
        _state = state;
    }

    public override async Task TriggerAsync(SocketMessageComponent smc)
    {
        await _onClick(smc, _state!);
    }
}