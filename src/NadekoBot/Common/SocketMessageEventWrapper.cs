#nullable disable
namespace NadekoBot.Common;

public sealed class ReactionEventWrapper : IDisposable
{
    public event Action<SocketReaction> OnReactionAdded = delegate { };
    public event Action<SocketReaction> OnReactionRemoved = delegate { };
    public event Action OnReactionsCleared = delegate { };

    public IUserMessage Message { get; }
    private readonly DiscordSocketClient _client;

    private bool disposing;

    public ReactionEventWrapper(DiscordSocketClient client, IUserMessage msg)
    {
        Message = msg ?? throw new ArgumentNullException(nameof(msg));
        _client = client;

        _client.ReactionAdded += Discord_ReactionAdded;
        _client.ReactionRemoved += Discord_ReactionRemoved;
        _client.ReactionsCleared += Discord_ReactionsCleared;
    }

    public void Dispose()
    {
        if (disposing)
            return;
        disposing = true;
        UnsubAll();
    }

    private Task Discord_ReactionsCleared(Cacheable<IUserMessage, ulong> msg, Cacheable<IMessageChannel, ulong> channel)
    {
        Task.Run(() =>
        {
            try
            {
                if (msg.Id == Message.Id)
                    OnReactionsCleared?.Invoke();
            }
            catch { }
        });

        return Task.CompletedTask;
    }

    private Task Discord_ReactionRemoved(
        Cacheable<IUserMessage, ulong> msg,
        Cacheable<IMessageChannel, ulong> cacheable,
        SocketReaction reaction)
    {
        Task.Run(() =>
        {
            try
            {
                if (msg.Id == Message.Id)
                    OnReactionRemoved?.Invoke(reaction);
            }
            catch { }
        });

        return Task.CompletedTask;
    }

    private Task Discord_ReactionAdded(
        Cacheable<IUserMessage, ulong> msg,
        Cacheable<IMessageChannel, ulong> cacheable,
        SocketReaction reaction)
    {
        Task.Run(() =>
        {
            try
            {
                if (msg.Id == Message.Id)
                    OnReactionAdded?.Invoke(reaction);
            }
            catch
            {
            }
        });

        return Task.CompletedTask;
    }

    public void UnsubAll()
    {
        _client.ReactionAdded -= Discord_ReactionAdded;
        _client.ReactionRemoved -= Discord_ReactionRemoved;
        _client.ReactionsCleared -= Discord_ReactionsCleared;
        OnReactionAdded = null;
        OnReactionRemoved = null;
        OnReactionsCleared = null;
    }
}