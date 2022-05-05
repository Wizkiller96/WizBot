namespace NadekoBot;

/// <summary>
/// Builder class for NadekoInteractions
/// </summary>
public class NadekoInteractionBuilder
{
    private NadekoInteractionData? iData;
    private Func<SocketMessageComponent, Task>? action;
    // private bool isOwn;

    public NadekoInteractionBuilder WithData<T>(in T data)
        where T : NadekoInteractionData
    {
        iData = data;
        return this;
    }

    // public NadekoOwnInteractionBuiler WithIsOwn(bool isOwn = true)
    // {
    //     this.isOwn = isOwn;
    //     return this;
    // }
    
    public NadekoInteractionBuilder WithAction(in Func<SocketMessageComponent, Task> fn)
    {
        this.action = fn;
        return this;
    }

    public NadekoActionInteraction Build(DiscordSocketClient client, ulong userId)
    {
        if (iData is null)
            throw new InvalidOperationException("You have to specify the data before building the interaction");

        if (action is null)
            throw new InvalidOperationException("You have to specify the action before building the interaction");

        return new(client, userId, iData, action);
    }
}