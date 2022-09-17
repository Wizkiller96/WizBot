#nullable disable
namespace NadekoBot.Modules.Administration.Services;

public class PruneService : INService
{
    //channelids where prunes are currently occuring
    private readonly ConcurrentHashSet<ulong> _pruningGuilds = new();
    private readonly TimeSpan _twoWeeks = TimeSpan.FromDays(14);
    private readonly ILogCommandService _logService;

    public PruneService(ILogCommandService logService)
        => _logService = logService;

    public async Task PruneWhere(ITextChannel channel, int amount, Func<IMessage, bool> predicate, ulong? after = null)
    {
        ArgumentNullException.ThrowIfNull(channel, nameof(channel));

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        if (!_pruningGuilds.Add(channel.GuildId))
            return;

        try
        {
            var now = DateTime.UtcNow;
            IMessage[] msgs;
            IMessage lastMessage = null;
            var dled = await channel.GetMessagesAsync(50).FlattenAsync();
            
            msgs = dled
                .Where(predicate)
                .Where(x => after is ulong a ? x.Id > a : true)
                .Take(amount)
                .ToArray();
            
            while (amount > 0 && msgs.Any())
            {
                lastMessage = msgs[^1];

                var bulkDeletable = new List<IMessage>();
                var singleDeletable = new List<IMessage>();
                foreach (var x in msgs)
                {
                    _logService.AddDeleteIgnore(x.Id);

                    if (now - x.CreatedAt < _twoWeeks)
                        bulkDeletable.Add(x);
                    else
                        singleDeletable.Add(x);
                }

                if (bulkDeletable.Count > 0)
                {
                    await channel.DeleteMessagesAsync(bulkDeletable);
                    await Task.Delay(2000);
                }

                foreach (var group in singleDeletable.Chunk(5))
                {
                    await group.Select(x => x.DeleteAsync()).WhenAll();
                    await Task.Delay(5000);
                }

                //this isn't good, because this still work as if i want to remove only specific user's messages from the last
                //100 messages, Maybe this needs to be reduced by msgs.Length instead of 100
                amount -= 50;
                if (amount > 0)
                {
                    dled = await channel.GetMessagesAsync(lastMessage, Direction.Before, 50).FlattenAsync();

                    msgs = dled
                        .Where(predicate)
                        .Where(x => after is ulong a ? x.Id > a : true)
                        .Take(amount)
                        .ToArray();
                }
            }
        }
        catch
        {
            //ignore
        }
        finally
        {
            _pruningGuilds.TryRemove(channel.GuildId);
        }
    }
}