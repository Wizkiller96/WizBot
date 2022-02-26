#nullable disable
using NadekoBot.Modules.Utility.Services;

namespace NadekoBot.Modules.Utility;

public partial class Utility
{
    [Group]
    public partial class InviteCommands : NadekoModule<InviteService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [BotPerm(ChannelPerm.CreateInstantInvite)]
        [UserPerm(ChannelPerm.CreateInstantInvite)]
        [NadekoOptions(typeof(InviteService.Options))]
        public async partial Task InviteCreate(params string[] args)
        {
            var (opts, success) = OptionsParser.ParseFrom(new InviteService.Options(), args);
            if (!success)
                return;

            var ch = (ITextChannel)ctx.Channel;
            var invite = await ch.CreateInviteAsync(opts.Expire, opts.MaxUses, opts.Temporary, opts.Unique);

            await SendConfirmAsync($"{ctx.User.Mention} https://discord.gg/{invite.Code}");
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [BotPerm(ChannelPerm.ManageChannels)]
        [UserPerm(ChannelPerm.ManageChannels)]
        public async partial Task InviteList(int page = 1, [Leftover] ITextChannel ch = null)
        {
            if (--page < 0)
                return;
            var channel = ch ?? (ITextChannel)ctx.Channel;

            var invites = await channel.GetInvitesAsync();

            await ctx.SendPaginatedConfirmAsync(page,
                cur =>
                {
                    var i = 1;
                    var invs = invites.Skip(cur * 9).Take(9).ToList();

                    if (!invs.Any())
                        return _eb.Create().WithErrorColor().WithDescription(GetText(strs.no_invites));

                    var embed = _eb.Create().WithOkColor();
                    foreach (var inv in invites)
                    {
                        var expiryString = inv.MaxAge is null or 0 || inv.CreatedAt is null
                            ? "∞"
                            : (inv.CreatedAt.Value.AddSeconds(inv.MaxAge.Value).UtcDateTime - DateTime.UtcNow).ToString(
                                @"d\.hh\:mm\:ss");
                        var creator = inv.Inviter.ToString().TrimTo(25);
                        var usesString = $"{inv.Uses} / {(inv.MaxUses == 0 ? "∞" : inv.MaxUses?.ToString())}";

                        var desc = $@"`{GetText(strs.inv_uses)}` **{usesString}**
`{GetText(strs.inv_expire)}` **{expiryString}**

{inv.Url} ";
                        embed.AddField($"#{i++} {creator}", desc);
                    }

                    return embed;
                },
                invites.Count,
                9);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [BotPerm(ChannelPerm.ManageChannels)]
        [UserPerm(ChannelPerm.ManageChannels)]
        public async partial Task InviteDelete(int index)
        {
            if (--index < 0)
                return;

            var ch = (ITextChannel)ctx.Channel;

            var invites = await ch.GetInvitesAsync();

            if (invites.Count <= index)
                return;
            var inv = invites.ElementAt(index);
            await inv.DeleteAsync();

            await ReplyAsync(GetText(strs.invite_deleted(Format.Bold(inv.Code))));
        }
    }
}