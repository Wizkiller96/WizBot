#nullable disable
using Microsoft.EntityFrameworkCore;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Db;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Permissions.Services;

public sealed class FilterService : IExecOnMessage
{
    public ConcurrentHashSet<ulong> InviteFilteringChannels { get; }
    public ConcurrentHashSet<ulong> InviteFilteringServers { get; }

    //serverid, filteredwords
    public ConcurrentDictionary<ulong, ConcurrentHashSet<string>> ServerFilteredWords { get; }

    public ConcurrentHashSet<ulong> WordFilteringChannels { get; }
    public ConcurrentHashSet<ulong> WordFilteringServers { get; }

    public ConcurrentHashSet<ulong> LinkFilteringChannels { get; }
    public ConcurrentHashSet<ulong> LinkFilteringServers { get; }

    public int Priority
        => int.MaxValue - 1;

    private readonly DbService _db;

    public FilterService(DiscordSocketClient client, DbService db)
    {
        _db = db;

        using (var uow = db.GetDbContext())
        {
            var ids = client.GetGuildIds();
            var configs = uow.Set<GuildConfig>()
                             .AsQueryable()
                             .Include(x => x.FilteredWords)
                             .Include(x => x.FilterLinksChannelIds)
                             .Include(x => x.FilterWordsChannelIds)
                             .Include(x => x.FilterInvitesChannelIds)
                             .Where(gc => ids.Contains(gc.GuildId))
                             .ToList();

            InviteFilteringServers = new(configs.Where(gc => gc.FilterInvites).Select(gc => gc.GuildId));
            InviteFilteringChannels =
                new(configs.SelectMany(gc => gc.FilterInvitesChannelIds.Select(fci => fci.ChannelId)));

            LinkFilteringServers = new(configs.Where(gc => gc.FilterLinks).Select(gc => gc.GuildId));
            LinkFilteringChannels =
                new(configs.SelectMany(gc => gc.FilterLinksChannelIds.Select(fci => fci.ChannelId)));

            var dict = configs.ToDictionary(gc => gc.GuildId,
                gc => new ConcurrentHashSet<string>(gc.FilteredWords.Select(fw => fw.Word)));

            ServerFilteredWords = new(dict);

            var serverFiltering = configs.Where(gc => gc.FilterWords);
            WordFilteringServers = new(serverFiltering.Select(gc => gc.GuildId));
            WordFilteringChannels =
                new(configs.SelectMany(gc => gc.FilterWordsChannelIds.Select(fwci => fwci.ChannelId)));
        }

        client.MessageUpdated += (oldData, newMsg, channel) =>
        {
            _ = Task.Run(() =>
            {
                var guild = (channel as ITextChannel)?.Guild;

                if (guild is null || newMsg is not IUserMessage usrMsg)
                    return Task.CompletedTask;

                return ExecOnMessageAsync(guild, usrMsg);
            });
            return Task.CompletedTask;
        };
    }

    public ConcurrentHashSet<string> FilteredWordsForChannel(ulong channelId, ulong guildId)
    {
        var words = new ConcurrentHashSet<string>();
        if (WordFilteringChannels.Contains(channelId))
            ServerFilteredWords.TryGetValue(guildId, out words);
        return words;
    }

    public void ClearFilteredWords(ulong guildId)
    {
        using var uow = _db.GetDbContext();
        var gc = uow.GuildConfigsForId(guildId,
            set => set.Include(x => x.FilteredWords).Include(x => x.FilterWordsChannelIds));

        WordFilteringServers.TryRemove(guildId);
        ServerFilteredWords.TryRemove(guildId, out _);

        foreach (var c in gc.FilterWordsChannelIds)
            WordFilteringChannels.TryRemove(c.ChannelId);

        gc.FilterWords = false;
        gc.FilteredWords.Clear();
        gc.FilterWordsChannelIds.Clear();

        uow.SaveChanges();
    }

    public ConcurrentHashSet<string> FilteredWordsForServer(ulong guildId)
    {
        var words = new ConcurrentHashSet<string>();
        if (WordFilteringServers.Contains(guildId))
            ServerFilteredWords.TryGetValue(guildId, out words);
        return words;
    }

    public async Task<bool> ExecOnMessageAsync(IGuild guild, IUserMessage msg)
    {
        if (msg.Author is not IGuildUser gu || gu.GuildPermissions.Administrator)
            return false;

        var results = await Task.WhenAll(FilterInvites(guild, msg), FilterWords(guild, msg), FilterLinks(guild, msg));

        return results.Any(x => x);
    }

    private async Task<bool> FilterWords(IGuild guild, IUserMessage usrMsg)
    {
        if (guild is null)
            return false;
        if (usrMsg is null)
            return false;

        var filteredChannelWords =
            FilteredWordsForChannel(usrMsg.Channel.Id, guild.Id) ?? new ConcurrentHashSet<string>();
        var filteredServerWords = FilteredWordsForServer(guild.Id) ?? new ConcurrentHashSet<string>();
        var wordsInMessage = usrMsg.Content.ToLowerInvariant().Split(' ');
        if (filteredChannelWords.Count != 0 || filteredServerWords.Count != 0)
        {
            foreach (var word in wordsInMessage)
            {
                if (filteredChannelWords.Contains(word) || filteredServerWords.Contains(word))
                {
                    Log.Information("User {UserName} [{UserId}] used a filtered word in {ChannelId} channel",
                        usrMsg.Author.ToString(),
                        usrMsg.Author.Id,
                        usrMsg.Channel.Id);

                    try
                    {
                        await usrMsg.DeleteAsync();
                    }
                    catch (HttpException ex)
                    {
                        Log.Warning(ex,
                            "I do not have permission to filter words in channel with id {Id}",
                            usrMsg.Channel.Id);
                    }

                    return true;
                }
            }
        }

        return false;
    }

    private async Task<bool> FilterInvites(IGuild guild, IUserMessage usrMsg)
    {
        if (guild is null)
            return false;
        if (usrMsg is null)
            return false;

        if ((InviteFilteringChannels.Contains(usrMsg.Channel.Id) || InviteFilteringServers.Contains(guild.Id))
            && usrMsg.Content.IsDiscordInvite())
        {
            Log.Information("User {UserName} [{UserId}] sent a filtered invite to {ChannelId} channel",
                usrMsg.Author.ToString(),
                usrMsg.Author.Id,
                usrMsg.Channel.Id);

            try
            {
                await usrMsg.DeleteAsync();
                return true;
            }
            catch (HttpException ex)
            {
                Log.Warning(ex,
                    "I do not have permission to filter invites in channel with id {Id}",
                    usrMsg.Channel.Id);
                return true;
            }
        }

        return false;
    }

    private async Task<bool> FilterLinks(IGuild guild, IUserMessage usrMsg)
    {
        if (guild is null)
            return false;
        if (usrMsg is null)
            return false;

        if ((LinkFilteringChannels.Contains(usrMsg.Channel.Id) || LinkFilteringServers.Contains(guild.Id))
            && usrMsg.Content.TryGetUrlPath(out _))
        {
            Log.Information("User {UserName} [{UserId}] sent a filtered link to {ChannelId} channel",
                usrMsg.Author.ToString(),
                usrMsg.Author.Id,
                usrMsg.Channel.Id);

            try
            {
                await usrMsg.DeleteAsync();
                return true;
            }
            catch (HttpException ex)
            {
                Log.Warning(ex, "I do not have permission to filter links in channel with id {Id}", usrMsg.Channel.Id);
                return true;
            }
        }

        return false;
    }
}