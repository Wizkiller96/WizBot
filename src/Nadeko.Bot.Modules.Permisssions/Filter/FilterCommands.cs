#nullable disable
using Microsoft.EntityFrameworkCore;
using NadekoBot.Db;
using NadekoBot.Modules.Permissions.Services;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Permissions;

public partial class Permissions
{
    [Group]
    public partial class FilterCommands : NadekoModule<FilterService>
    {
        private readonly DbService _db;

        public FilterCommands(DbService db)
            => _db = db;

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task FwClear()
        {
            _service.ClearFilteredWords(ctx.Guild.Id);
            await ReplyConfirmLocalizedAsync(strs.fw_cleared);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task FilterList()
        {
            var embed = _eb.Create(ctx)
                .WithOkColor()
                .WithTitle("Server filter settings");

            var config = await _service.GetFilterSettings(ctx.Guild.Id);

            string GetEnabledEmoji(bool value)
                => value ? "\\🟢" : "\\🔴";

            async Task<string> GetChannelListAsync(IReadOnlyCollection<ulong> channels)
            {
                var toReturn = (await channels
                        .Select(async cid =>
                        {
                            var ch = await ctx.Guild.GetChannelAsync(cid);
                            return ch is null
                                ? $"{cid} *missing*"
                                : $"<#{cid}>";
                        })
                        .WhenAll())
                    .Join('\n');

                if (string.IsNullOrWhiteSpace(toReturn))
                    return GetText(strs.no_channel_found);

                return toReturn;
            }

            embed.AddField($"{GetEnabledEmoji(config.FilterLinksEnabled)} Filter Links",
                await GetChannelListAsync(config.FilterLinksChannels));

            embed.AddField($"{GetEnabledEmoji(config.FilterInvitesEnabled)} Filter Invites",
                await GetChannelListAsync(config.FilterInvitesChannels));

            await ctx.Channel.EmbedAsync(embed);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task SrvrFilterInv()
        {
            var channel = (ITextChannel)ctx.Channel;

            bool enabled;
            await using (var uow = _db.GetDbContext())
            {
                var config = uow.GuildConfigsForId(channel.Guild.Id, set => set);
                enabled = config.FilterInvites = !config.FilterInvites;
                await uow.SaveChangesAsync();
            }

            if (enabled)
            {
                _service.InviteFilteringServers.Add(channel.Guild.Id);
                await ReplyConfirmLocalizedAsync(strs.invite_filter_server_on);
            }
            else
            {
                _service.InviteFilteringServers.TryRemove(channel.Guild.Id);
                await ReplyConfirmLocalizedAsync(strs.invite_filter_server_off);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task ChnlFilterInv()
        {
            var channel = (ITextChannel)ctx.Channel;

            FilterChannelId removed;
            await using (var uow = _db.GetDbContext())
            {
                var config = uow.GuildConfigsForId(channel.Guild.Id,
                    set => set.Include(gc => gc.FilterInvitesChannelIds));
                var match = new FilterChannelId
                {
                    ChannelId = channel.Id
                };
                removed = config.FilterInvitesChannelIds.FirstOrDefault(fc => fc.Equals(match));

                if (removed is null)
                    config.FilterInvitesChannelIds.Add(match);
                else
                    uow.Remove(removed);
                await uow.SaveChangesAsync();
            }

            if (removed is null)
            {
                _service.InviteFilteringChannels.Add(channel.Id);
                await ReplyConfirmLocalizedAsync(strs.invite_filter_channel_on);
            }
            else
            {
                _service.InviteFilteringChannels.TryRemove(channel.Id);
                await ReplyConfirmLocalizedAsync(strs.invite_filter_channel_off);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task SrvrFilterLin()
        {
            var channel = (ITextChannel)ctx.Channel;

            bool enabled;
            await using (var uow = _db.GetDbContext())
            {
                var config = uow.GuildConfigsForId(channel.Guild.Id, set => set);
                enabled = config.FilterLinks = !config.FilterLinks;
                await uow.SaveChangesAsync();
            }

            if (enabled)
            {
                _service.LinkFilteringServers.Add(channel.Guild.Id);
                await ReplyConfirmLocalizedAsync(strs.link_filter_server_on);
            }
            else
            {
                _service.LinkFilteringServers.TryRemove(channel.Guild.Id);
                await ReplyConfirmLocalizedAsync(strs.link_filter_server_off);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task ChnlFilterLin()
        {
            var channel = (ITextChannel)ctx.Channel;

            FilterLinksChannelId removed;
            await using (var uow = _db.GetDbContext())
            {
                var config =
                    uow.GuildConfigsForId(channel.Guild.Id, set => set.Include(gc => gc.FilterLinksChannelIds));
                var match = new FilterLinksChannelId
                {
                    ChannelId = channel.Id
                };
                removed = config.FilterLinksChannelIds.FirstOrDefault(fc => fc.Equals(match));

                if (removed is null)
                    config.FilterLinksChannelIds.Add(match);
                else
                    uow.Remove(removed);
                await uow.SaveChangesAsync();
            }

            if (removed is null)
            {
                _service.LinkFilteringChannels.Add(channel.Id);
                await ReplyConfirmLocalizedAsync(strs.link_filter_channel_on);
            }
            else
            {
                _service.LinkFilteringChannels.TryRemove(channel.Id);
                await ReplyConfirmLocalizedAsync(strs.link_filter_channel_off);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task SrvrFilterWords()
        {
            var channel = (ITextChannel)ctx.Channel;

            bool enabled;
            await using (var uow = _db.GetDbContext())
            {
                var config = uow.GuildConfigsForId(channel.Guild.Id, set => set);
                enabled = config.FilterWords = !config.FilterWords;
                await uow.SaveChangesAsync();
            }

            if (enabled)
            {
                _service.WordFilteringServers.Add(channel.Guild.Id);
                await ReplyConfirmLocalizedAsync(strs.word_filter_server_on);
            }
            else
            {
                _service.WordFilteringServers.TryRemove(channel.Guild.Id);
                await ReplyConfirmLocalizedAsync(strs.word_filter_server_off);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task ChnlFilterWords()
        {
            var channel = (ITextChannel)ctx.Channel;

            FilterWordsChannelId removed;
            await using (var uow = _db.GetDbContext())
            {
                var config =
                    uow.GuildConfigsForId(channel.Guild.Id, set => set.Include(gc => gc.FilterWordsChannelIds));

                var match = new FilterWordsChannelId
                {
                    ChannelId = channel.Id
                };
                removed = config.FilterWordsChannelIds.FirstOrDefault(fc => fc.Equals(match));
                if (removed is null)
                    config.FilterWordsChannelIds.Add(match);
                else
                    uow.Remove(removed);
                await uow.SaveChangesAsync();
            }

            if (removed is null)
            {
                _service.WordFilteringChannels.Add(channel.Id);
                await ReplyConfirmLocalizedAsync(strs.word_filter_channel_on);
            }
            else
            {
                _service.WordFilteringChannels.TryRemove(channel.Id);
                await ReplyConfirmLocalizedAsync(strs.word_filter_channel_off);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task FilterWord([Leftover] string word)
        {
            var channel = (ITextChannel)ctx.Channel;

            word = word?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(word))
                return;

            FilteredWord removed;
            await using (var uow = _db.GetDbContext())
            {
                var config = uow.GuildConfigsForId(channel.Guild.Id, set => set.Include(gc => gc.FilteredWords));

                removed = config.FilteredWords.FirstOrDefault(fw => fw.Word.Trim().ToLowerInvariant() == word);

                if (removed is null)
                {
                    config.FilteredWords.Add(new()
                    {
                        Word = word
                    });
                }
                else
                    uow.Remove(removed);

                await uow.SaveChangesAsync();
            }

            var filteredWords =
                _service.ServerFilteredWords.GetOrAdd(channel.Guild.Id, new ConcurrentHashSet<string>());

            if (removed is null)
            {
                filteredWords.Add(word);
                await ReplyConfirmLocalizedAsync(strs.filter_word_add(Format.Code(word)));
            }
            else
            {
                filteredWords.TryRemove(word);
                await ReplyConfirmLocalizedAsync(strs.filter_word_remove(Format.Code(word)));
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task LstFilterWords(int page = 1)
        {
            page--;
            if (page < 0)
                return;

            var channel = (ITextChannel)ctx.Channel;

            _service.ServerFilteredWords.TryGetValue(channel.Guild.Id, out var fwHash);

            var fws = fwHash.ToArray();

            await ctx.SendPaginatedConfirmAsync(page,
                curPage => _eb.Create()
                              .WithTitle(GetText(strs.filter_word_list))
                              .WithDescription(string.Join("\n", fws.Skip(curPage * 10).Take(10)))
                              .WithOkColor(),
                fws.Length,
                10);
        }
    }
}