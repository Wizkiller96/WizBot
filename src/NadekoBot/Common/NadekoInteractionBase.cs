namespace NadekoBot;


public abstract class NadekoInteraction
{
    // improvements:
    //  - state in OnAction
    //  - configurable delay
    //  - 
    public abstract string Name { get; }
    public abstract IEmote Emote { get; }
    public Func<SocketMessageComponent, Task> OnAction { get; }

    protected readonly DiscordSocketClient _client;

    protected readonly TaskCompletionSource<bool> _interactionCompletedSource;

    protected ulong _authorId;
    protected IUserMessage message;

    protected NadekoInteraction(DiscordSocketClient client, ulong authorId, Func<SocketMessageComponent, Task> onAction)
    {
        _client = client;
        _authorId = authorId;
        OnAction = onAction;
        _interactionCompletedSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public async Task RunAsync(IUserMessage msg)
    {
        message = msg;

        _client.InteractionCreated += OnInteraction;
        await Task.WhenAny(Task.Delay(10_000), _interactionCompletedSource.Task);
        _client.InteractionCreated -= OnInteraction;

        await msg.ModifyAsync(m => m.Components = new ComponentBuilder().Build());
    }

    private async Task OnInteraction(SocketInteraction arg)
    {
        if (arg is not SocketMessageComponent smc)
            return;

        if (smc.Message.Id != message.Id)
            return;

        if (smc.Data.CustomId != Name)
            return;

        if (smc.User.Id != _authorId)
        {
            await arg.DeferAsync();
            return;
        }

        _ = Task.Run(async () =>
        {
            await OnAction(smc);
            
            // this should only be a thing on single-response buttons
            _interactionCompletedSource.TrySetResult(true);

            if (!smc.HasResponded)
            {
                await smc.DeferAsync();
            }
        });
    }


    public MessageComponent CreateComponent()
    {
        var comp = new ComponentBuilder()
            .WithButton(new ButtonBuilder(style: ButtonStyle.Secondary, emote: Emote, customId: Name));

        return comp.Build();
    }
}
    