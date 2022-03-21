#nullable disable
using LinqToDB;
using Microsoft.Extensions.Caching.Memory;
using NadekoBot.Common.ModuleBehaviors;
using System.Net;
using System.Threading.Channels;

namespace NadekoBot.Modules.Administration.Services;

public sealed class ImageOnlyChannelService : IEarlyBehavior
{
    public int Priority { get; } = 0;
    private readonly IMemoryCache _ticketCache;
    private readonly DiscordSocketClient _client;
    private readonly DbService _db;
    private readonly ConcurrentDictionary<ulong, ConcurrentHashSet<ulong>> _enabledOn;

    private readonly Channel<IUserMessage> _deleteQueue = Channel.CreateBounded<IUserMessage>(
        new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });


    public ImageOnlyChannelService(IMemoryCache ticketCache, DiscordSocketClient client, DbService db)
    {
        _ticketCache = ticketCache;
        _client = client;
        _db = db;

        using var uow = _db.GetDbContext();
        _enabledOn = uow.ImageOnlyChannels.ToList()
                        .GroupBy(x => x.GuildId)
                        .ToDictionary(x => x.Key, x => new ConcurrentHashSet<ulong>(x.Select(y => y.ChannelId)))
                        .ToConcurrent();

        _ = Task.Run(DeleteQueueRunner);

        _client.ChannelDestroyed += ClientOnChannelDestroyed;
    }

    private Task ClientOnChannelDestroyed(SocketChannel ch)
    {
        if (ch is not IGuildChannel gch)
            return Task.CompletedTask;

        if (_enabledOn.TryGetValue(gch.GuildId, out var channels) && channels.TryRemove(ch.Id))
            ToggleImageOnlyChannel(gch.GuildId, ch.Id, true);

        return Task.CompletedTask;
    }

    private async Task DeleteQueueRunner()
    {
        while (true)
        {
            var toDelete = await _deleteQueue.Reader.ReadAsync();
            try
            {
                await toDelete.DeleteAsync();
                await Task.Delay(1000);
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.Forbidden)
            {
                // disable if bot can't delete messages in the channel
                ToggleImageOnlyChannel(((ITextChannel)toDelete.Channel).GuildId, toDelete.Channel.Id, true);
            }
        }
    }

    public bool ToggleImageOnlyChannel(ulong guildId, ulong channelId, bool forceDisable = false)
    {
        var newState = false;
        using var uow = _db.GetDbContext();
        if (forceDisable || (_enabledOn.TryGetValue(guildId, out var channels) && channels.TryRemove(channelId)))
            uow.ImageOnlyChannels.Delete(x => x.ChannelId == channelId);
        else
        {
            uow.ImageOnlyChannels.Add(new()
            {
                GuildId = guildId,
                ChannelId = channelId
            });

            channels = _enabledOn.GetOrAdd(guildId, new ConcurrentHashSet<ulong>());
            channels.Add(channelId);
            newState = true;
        }

        uow.SaveChanges();
        return newState;
    }

    public async Task<bool> RunBehavior(IGuild guild, IUserMessage msg)
    {
        if (msg.Channel is not ITextChannel tch)
            return false;

        if (msg.Attachments.Any(x => x is { Height: > 0, Width: > 0 }))
            return false;

        if (!_enabledOn.TryGetValue(tch.GuildId, out var chs) || !chs.Contains(msg.Channel.Id))
            return false;

        var user = await tch.Guild.GetUserAsync(msg.Author.Id)
                   ?? await _client.Rest.GetGuildUserAsync(tch.GuildId, msg.Author.Id);

        if (user is null)
            return false;

        // ignore owner and admin
        if (user.Id == tch.Guild.OwnerId || user.GuildPermissions.Administrator)
        {
            Log.Information("Image-Only: Ignoring owner od admin ({ChannelId})", msg.Channel.Id);
            return false;
        }

        // ignore users higher in hierarchy
        var botUser = await tch.Guild.GetCurrentUserAsync();
        if (user.GetRoles().Max(x => x.Position) >= botUser.GetRoles().Max(x => x.Position))
            return false;

        if (!botUser.GetPermissions(tch).ManageChannel)
        {
            ToggleImageOnlyChannel(tch.GuildId, tch.Id, true);
            return false;
        }

        var shouldLock = AddUserTicket(tch.GuildId, msg.Author.Id);
        if (shouldLock)
        {
            await tch.AddPermissionOverwriteAsync(msg.Author, new(sendMessages: PermValue.Deny));
            Log.Warning("Image-Only: User {User} [{UserId}] has been banned from typing in the channel [{ChannelId}]",
                msg.Author,
                msg.Author.Id,
                msg.Channel.Id);
        }

        try
        {
            await _deleteQueue.Writer.WriteAsync(msg);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting message {MessageId} in image-only channel {ChannelId}", msg.Id, tch.Id);
        }

        return true;
    }

    private bool AddUserTicket(ulong guildId, ulong userId)
    {
        var old = _ticketCache.GetOrCreate($"{guildId}_{userId}",
            entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
                return 0;
            });

        _ticketCache.Set($"{guildId}_{userId}", ++old);

        // if this is the third time that the user posts a
        // non image in an image-only channel on this server 
        return old > 2;
    }
}