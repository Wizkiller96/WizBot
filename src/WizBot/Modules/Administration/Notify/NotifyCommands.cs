using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using WizBot.Common.ModuleBehaviors;
using WizBot.Db.Models;

namespace WizBot.Modules.Administration;

public sealed class NotifyService : IReadyExecutor, INService
{
    private readonly DbService _db;
    private readonly IMessageSenderService _mss;
    private readonly DiscordSocketClient _client;
    private readonly IBotCreds _creds;

    public NotifyService(
        DbService db,
        IMessageSenderService mss,
        DiscordSocketClient client,
        IBotCreds creds)
    {
        _db = db;
        _mss = mss;
        _client = client;
        _creds = creds;
    }

    public async Task OnReadyAsync()
    {
        // .Where(x => Linq2DbExpressions.GuildOnShard(guildId,
        // _creds.TotalShards,
        // _client.ShardId))
    }

    public async Task EnableAsync(
        ulong guildId,
        ulong channelId,
        NotifyEvent nEvent,
        string message)
    {
        await using var uow = _db.GetDbContext();
        await uow.GetTable<Notify>()
                 .InsertOrUpdateAsync(() => new()
                     {
                         GuildId = guildId,
                         ChannelId = channelId,
                         Event = nEvent,
                         Message = message,
                     },
                     (_) => new()
                     {
                         Message = message,
                         ChannelId = channelId
                     },
                     () => new()
                     {
                         GuildId = guildId,
                         Event = nEvent
                     });
    }

    public async Task DisableAsync(ulong guildId, NotifyEvent nEvent)
    {
        await using var uow = _db.GetDbContext();
        var deleted = await uow.GetTable<Notify>()
                               .Where(x => x.GuildId == guildId && x.Event == nEvent)
                               .DeleteAsync();

        if (deleted > 0)
            return;
    }
}

public partial class Administration
{
    public class NotifyCommands : WizBotModule<NotifyService>
    {
        [Cmd]
        [OwnerOnly]
        public async Task Notify(NotifyEvent nEvent, [Leftover] string message = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                await _service.DisableAsync(ctx.Guild.Id, nEvent);
                await Response().Confirm(strs.notify_off(nEvent)).SendAsync();
                return;
            }

            await _service.EnableAsync(ctx.Guild.Id, ctx.Channel.Id, nEvent, message);
            await Response().Confirm(strs.notify_on(nEvent.ToString())).SendAsync();
        }
    }
}