namespace NadekoBot;

public class SimpleInteraction<T>
{
    public ButtonBuilder Button { get; }
    private readonly Func<SocketMessageComponent, T, Task> _onClick;
    private readonly T? _state;

    public SimpleInteraction(ButtonBuilder button, Func<SocketMessageComponent, T?, Task> onClick, T? state = default)
    {
        Button = button;
        _onClick = onClick;
        _state = state;
    }

    public async Task TriggerAsync(SocketMessageComponent smc)
    {
        await _onClick(smc, _state!);
    }
}