﻿namespace WizBot;

public sealed class WizBotInteraction
{
    private readonly ulong _authorId;
    private readonly ButtonBuilder _button;
    private readonly Func<SocketMessageComponent, Task> _onClick;
    private readonly bool _onlyAuthor;
    public DiscordSocketClient Client { get; }

    private readonly TaskCompletionSource<bool> _interactionCompletedSource;

    private IUserMessage message = null!;

    public WizBotInteraction(DiscordSocketClient client,
        ulong authorId,
        ButtonBuilder button,
        Func<SocketMessageComponent, Task> onClick,
        bool onlyAuthor)
    {
        _authorId = authorId;
        _button = button;
        _onClick = onClick;
        _onlyAuthor = onlyAuthor;
        _interactionCompletedSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        
        Client = client;
    }

    public async Task RunAsync(IUserMessage msg)
    {
        message = msg;

        Client.InteractionCreated += OnInteraction;
        await Task.WhenAny(Task.Delay(15_000), _interactionCompletedSource.Task);
        Client.InteractionCreated -= OnInteraction;

        await msg.ModifyAsync(m => m.Components = new ComponentBuilder().Build());
    }
    
    private async Task OnInteraction(SocketInteraction arg)
    {
        if (arg is not SocketMessageComponent smc)
            return;

        if (smc.Message.Id != message.Id)
            return;

        if (_onlyAuthor && smc.User.Id != _authorId)
            return;

        if (smc.Data.CustomId != _button.CustomId)
            return;

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
            .WithButton(_button);

        return comp.Build();
    }

    public Task ExecuteOnActionAsync(SocketMessageComponent smc)
        => _onClick(smc);
}