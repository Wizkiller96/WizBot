using System;
using Discord;
using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Common;
using NadekoBot.Modules.Utility.Services;
using NadekoBot.Extensions;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class InviteCommands : NadekoSubmodule<InviteService>
        {
            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [BotPerm(ChannelPerm.CreateInstantInvite)]
            [UserPerm(ChannelPerm.CreateInstantInvite)]
            [NadekoOptions(typeof(InviteService.Options))]
            public async Task InviteCreate(params string[] args)
            {
                var (opts, success) = OptionsParser.ParseFrom(new InviteService.Options(), args);
                if (!success)
                    return;

                var ch = (ITextChannel)ctx.Channel;
                var invite = await ch.CreateInviteAsync(opts.Expire, opts.MaxUses, isTemporary: opts.Temporary, isUnique: opts.Unique).ConfigureAwait(false);

                await SendConfirmAsync($"{ctx.User.Mention} https://discord.gg/{invite.Code}").ConfigureAwait(false);
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [BotPerm(ChannelPerm.ManageChannel)]
            [UserPerm(ChannelPerm.ManageChannel)]
            public async Task InviteList(int page = 1, [Leftover]ITextChannel ch = null)
            {
                if (--page < 0)
                    return;
                var channel = ch ?? (ITextChannel)ctx.Channel;

                var invites = await channel.GetInvitesAsync().ConfigureAwait(false);

                await ctx.SendPaginatedConfirmAsync(page, (cur) =>
                {
                    var i = 1;
                    var invs = invites
                        .Skip(cur * 9)
                        .Take(9)
                        .ToList();
                    
                    if (!invs.Any())
                    {
                        return _eb.Create()
                            .WithErrorColor()
                            .WithDescription(GetText(strs.no_invites));
                    }

                    var embed = _eb.Create().WithOkColor();
                    foreach (var inv in invites)
                    {
                        var expiryString = (inv.MaxAge is null || inv.MaxAge == 0 || inv.CreatedAt is null)
                            ? "∞"
                            : (inv.CreatedAt.Value.AddSeconds(inv.MaxAge.Value).UtcDateTime - DateTime.UtcNow)
                            .ToString(@"d\.hh\:mm\:ss");
                        var creator = inv.Inviter.ToString().TrimTo(25);
                        var usesString = $"{inv.Uses} / {(inv.MaxUses == 0 ? "∞" : inv.MaxUses?.ToString())}";
                        
                        var desc = $@"`{GetText(strs.inv_uses)}` **{usesString}**
`{GetText(strs.inv_expire)}` **{expiryString}**

{inv.Url} ";
                        embed.AddField($"#{i++} {creator}", desc);
                    }

                    return embed;
                    
                }, invites.Count, 9).ConfigureAwait(false);
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [BotPerm(ChannelPerm.ManageChannel)]
            [UserPerm(ChannelPerm.ManageChannel)]
            public async Task InviteDelete(int index)
            {
                if (--index < 0)
                    return;
                
                var ch = (ITextChannel)ctx.Channel;

                var invites = await ch.GetInvitesAsync().ConfigureAwait(false);

                if (invites.Count <= index)
                    return;
                var inv = invites.ElementAt(index);
                await inv.DeleteAsync().ConfigureAwait(false);

                await ReplyAsync(GetText(strs.invite_deleted(Format.Bold(inv.Code))));
            }
        }
    }
}
