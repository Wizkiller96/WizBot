namespace WizBot;

public abstract class WizBotButtonInteraction
{
    // improvements:
    //  - state in OnAction
    //  - configurable delay
    //  - 
    protected abstract string Name { get; }
    protected abstract IEmote Emote { get; }
    protected virtual string? Text { get; } = null;

    public DiscordSocketClient Client { get; }

    protected readonly TaskCompletionSource<bool> _interactionCompletedSource;

    protected IUserMessage message = null!;

    protected WizBotButtonInteraction(DiscordSocketClient client)
    {
        Client = client;
        _interactionCompletedSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public async Task RunAsync(IUserMessage msg)
    {
        message = msg;

        Client.InteractionCreated += OnInteraction;
        await Task.WhenAny(Task.Delay(10_000), _interactionCompletedSource.Task);
        Client.InteractionCreated -= OnInteraction;

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


    public virtual MessageComponent CreateComponent()
    {
        var comp = new ComponentBuilder()
            .WithButton(GetButtonBuilder());

        return comp.Build();
    }
    
    public ButtonBuilder GetButtonBuilder()
        => new ButtonBuilder(style: ButtonStyle.Secondary, emote: Emote, customId: Name, label: Text);

    public abstract Task ExecuteOnActionAsync(SocketMessageComponent smc);
}

// this is all so wrong ...