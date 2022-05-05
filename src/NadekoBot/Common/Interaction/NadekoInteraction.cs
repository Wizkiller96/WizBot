namespace NadekoBot;

public abstract class NadekoInteraction
{
    // improvements:
    //  - state in OnAction
    //  - configurable delay
    //  - 
    public abstract string Name { get; }
    public abstract IEmote Emote { get; }

    protected readonly DiscordSocketClient _client;

    protected readonly TaskCompletionSource<bool> _interactionCompletedSource;

    protected IUserMessage message = null!;

    protected NadekoInteraction(DiscordSocketClient client)
    {
        _client = client;
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

    protected abstract ValueTask<bool> Validate(SocketMessageComponent smc);
    private async Task OnInteraction(SocketInteraction arg)
    {
        if (arg is not SocketMessageComponent smc)
            return;

        if (smc.Message.Id != message.Id)
            return;

        if (smc.Data.CustomId != Name)
            return;

        if (!await Validate(smc))
        {
            await smc.DeferAsync();
            return;
        }

        _ = Task.Run(async () =>
        {
            await ExecuteOnActionAsync(smc);
            
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

    public abstract Task ExecuteOnActionAsync(SocketMessageComponent smc);
}