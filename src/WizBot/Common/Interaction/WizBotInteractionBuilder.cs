namespace WizBot;

/// <summary>
/// Builder class for WizBotInteractions
/// </summary>
public class WizBotInteractionBuilder
{
    private WizBotInteractionData? iData;
    private Func<SocketMessageComponent, Task>? action;
    // private bool isOwn;

    public WizBotInteractionBuilder WithData<T>(in T data)
        where T : WizBotInteractionData
    {
        iData = data;
        return this;
    }

    // public WizBotOwnInteractionBuiler WithIsOwn(bool isOwn = true)
    // {
    //     this.isOwn = isOwn;
    //     return this;
    
    // }
    
    public WizBotInteractionBuilder WithAction(in Func<SocketMessageComponent, Task> fn)
    {
        this.action = fn;
        return this;
    }

    public WizBotButtonActionInteraction Build(DiscordSocketClient client, ulong userId)
    {
        if (iData is null)
            throw new InvalidOperationException("You have to specify the data before building the interaction");

        if (action is null)
            throw new InvalidOperationException("You have to specify the action before building the interaction");

        return new(client, userId, iData, action);
    }
}