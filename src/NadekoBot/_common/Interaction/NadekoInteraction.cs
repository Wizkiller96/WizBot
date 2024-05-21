﻿namespace NadekoBot;

public abstract class NadekoInteractionBase
{
    private readonly ulong _authorId;
    private readonly Func<SocketMessageComponent, Task> _onAction;
    private readonly bool _onlyAuthor;
    public DiscordSocketClient Client { get; }

    private readonly TaskCompletionSource<bool> _interactionCompletedSource;

    private IUserMessage message = null!;
    private readonly string _customId;
    private readonly bool _singleUse;

    public NadekoInteractionBase(
        DiscordSocketClient client,
        ulong authorId,
        string customId,
        Func<SocketMessageComponent, Task> onAction,
        bool onlyAuthor,
        bool singleUse = true)
    {
        _authorId = authorId;
        _customId = customId;
        _onAction = onAction;
        _onlyAuthor = onlyAuthor;
        _singleUse = singleUse;
        _interactionCompletedSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Client = client;
    }

    public async Task RunAsync(IUserMessage msg)
    {
        message = msg;

        Client.InteractionCreated += OnInteraction;
        if (_singleUse)
            await Task.WhenAny(Task.Delay(30_000), _interactionCompletedSource.Task);
        else
            await Task.Delay(30_000);
        Client.InteractionCreated -= OnInteraction;

        await msg.ModifyAsync(m => m.Components = new ComponentBuilder().Build());
    }

    private Task OnInteraction(SocketInteraction arg)
    {
        if (arg is not SocketMessageComponent smc)
            return Task.CompletedTask;

        if (smc.Message.Id != message.Id)
            return Task.CompletedTask;

        if (_onlyAuthor && smc.User.Id != _authorId)
            return Task.CompletedTask;

        if (smc.Data.CustomId != _customId)
            return Task.CompletedTask;

        _ = Task.Run(async () =>
        {
            try
            {
                _interactionCompletedSource.TrySetResult(true);
                await ExecuteOnActionAsync(smc);

                if (!smc.HasResponded)
                {
                    await smc.DeferAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "An exception occured while handling an interaction: {Message}", ex.Message);
            }
        });

        return Task.CompletedTask;
    }


    public abstract void AddTo(ComponentBuilder cb);

    public Task ExecuteOnActionAsync(SocketMessageComponent smc)
        => _onAction(smc);
}

public sealed class NadekoModalSubmitHandler
{
    private readonly ulong _authorId;
    private readonly Func<SocketModal, Task> _onAction;
    private readonly bool _onlyAuthor;
    public DiscordSocketClient Client { get; }

    private readonly TaskCompletionSource<bool> _interactionCompletedSource;

    private IUserMessage message = null!;
    private readonly string _customId;

    public NadekoModalSubmitHandler(
        DiscordSocketClient client,
        ulong authorId,
        string customId,
        Func<SocketModal, Task> onAction,
        bool onlyAuthor)
    {
        _authorId = authorId;
        _customId = customId;
        _onAction = onAction;
        _onlyAuthor = onlyAuthor;
        _interactionCompletedSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Client = client;
    }

    public async Task RunAsync(IUserMessage msg)
    {
        message = msg;

        Client.ModalSubmitted += OnInteraction;
        await Task.WhenAny(Task.Delay(300_000), _interactionCompletedSource.Task);
        Client.ModalSubmitted -= OnInteraction;

        await msg.ModifyAsync(m => m.Components = new ComponentBuilder().Build());
    }

    private Task OnInteraction(SocketModal sm)
    {
        if (sm.Message.Id != message.Id)
            return Task.CompletedTask;

        if (_onlyAuthor && sm.User.Id != _authorId)
            return Task.CompletedTask;

        if (sm.Data.CustomId != _customId)
            return Task.CompletedTask;

        _ = Task.Run(async () =>
        {
            try
            {
                _interactionCompletedSource.TrySetResult(true);
                await ExecuteOnActionAsync(sm);

                if (!sm.HasResponded)
                {
                    await sm.DeferAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "An exception occured while handling a: {Message}", ex.Message);
            }
        });

        return Task.CompletedTask;
    }


    public Task ExecuteOnActionAsync(SocketModal smd)
        => _onAction(smd);
}