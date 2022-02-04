#nullable disable
using NadekoBot.Common.TypeReaders.Models;
using NadekoBot.Modules.Administration.Services;

namespace NadekoBot.Modules.Administration;

public partial class Administration : NadekoModule<AdministrationService>
{
    public enum Channel
    {
        Channel,
        Ch,
        Chnl,
        Chan
    }

    public enum List
    {
        List = 0,
        Ls = 0
    }

    public enum Server
    {
        Server
    }

    public enum State
    {
        Enable,
        Disable,
        Inherit
    }

    private readonly ImageOnlyChannelService _imageOnly;

    public Administration(ImageOnlyChannelService imageOnly)
        => _imageOnly = imageOnly;

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.Administrator)]
    [BotPerm(GuildPerm.ManageGuild)]
    public async partial Task ImageOnlyChannel(StoopidTime time = null)
    {
        var newValue = _imageOnly.ToggleImageOnlyChannel(ctx.Guild.Id, ctx.Channel.Id);
        if (newValue)
            await ReplyConfirmLocalizedAsync(strs.imageonly_enable);
        else
            await ReplyPendingLocalizedAsync(strs.imageonly_disable);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(ChannelPerm.ManageChannels)]
    [BotPerm(ChannelPerm.ManageChannels)]
    public async partial Task Slowmode(StoopidTime time = null)
    {
        var seconds = (int?)time?.Time.TotalSeconds ?? 0;
        if (time is not null && (time.Time < TimeSpan.FromSeconds(0) || time.Time > TimeSpan.FromHours(6)))
            return;

        await ((ITextChannel)ctx.Channel).ModifyAsync(tcp =>
        {
            tcp.SlowModeInterval = seconds;
        });

        await ctx.OkAsync();
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.Administrator)]
    [BotPerm(GuildPerm.ManageMessages)]
    [Priority(2)]
    public async partial Task Delmsgoncmd(List _)
    {
        var guild = (SocketGuild)ctx.Guild;
        var (enabled, channels) = _service.GetDelMsgOnCmdData(ctx.Guild.Id);

        var embed = _eb.Create()
                       .WithOkColor()
                       .WithTitle(GetText(strs.server_delmsgoncmd))
                       .WithDescription(enabled ? "✅" : "❌");

        var str = string.Join("\n",
            channels.Select(x =>
            {
                var ch = guild.GetChannel(x.ChannelId)?.ToString() ?? x.ChannelId.ToString();
                var prefixSign = x.State ? "✅ " : "❌ ";
                return prefixSign + ch;
            }));

        if (string.IsNullOrWhiteSpace(str))
            str = "-";

        embed.AddField(GetText(strs.channel_delmsgoncmd), str);

        await ctx.Channel.EmbedAsync(embed);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.Administrator)]
    [BotPerm(GuildPerm.ManageMessages)]
    [Priority(1)]
    public async partial Task Delmsgoncmd(Server _ = Server.Server)
    {
        if (_service.ToggleDeleteMessageOnCommand(ctx.Guild.Id))
        {
            _service.DeleteMessagesOnCommand.Add(ctx.Guild.Id);
            await ReplyConfirmLocalizedAsync(strs.delmsg_on);
        }
        else
        {
            _service.DeleteMessagesOnCommand.TryRemove(ctx.Guild.Id);
            await ReplyConfirmLocalizedAsync(strs.delmsg_off);
        }
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.Administrator)]
    [BotPerm(GuildPerm.ManageMessages)]
    [Priority(0)]
    public partial Task Delmsgoncmd(Channel _, State s, ITextChannel ch)
        => Delmsgoncmd(_, s, ch.Id);

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.Administrator)]
    [BotPerm(GuildPerm.ManageMessages)]
    [Priority(1)]
    public async partial Task Delmsgoncmd(Channel _, State s, ulong? chId = null)
    {
        var actualChId = chId ?? ctx.Channel.Id;
        await _service.SetDelMsgOnCmdState(ctx.Guild.Id, actualChId, s);

        if (s == State.Disable)
            await ReplyConfirmLocalizedAsync(strs.delmsg_channel_off);
        else if (s == State.Enable)
            await ReplyConfirmLocalizedAsync(strs.delmsg_channel_on);
        else
            await ReplyConfirmLocalizedAsync(strs.delmsg_channel_inherit);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.DeafenMembers)]
    [BotPerm(GuildPerm.DeafenMembers)]
    public async partial Task Deafen(params IGuildUser[] users)
    {
        await _service.DeafenUsers(true, users);
        await ReplyConfirmLocalizedAsync(strs.deafen);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.DeafenMembers)]
    [BotPerm(GuildPerm.DeafenMembers)]
    public async partial Task UnDeafen(params IGuildUser[] users)
    {
        await _service.DeafenUsers(false, users);
        await ReplyConfirmLocalizedAsync(strs.undeafen);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.ManageChannels)]
    [BotPerm(GuildPerm.ManageChannels)]
    public async partial Task DelVoiChanl([Leftover] IVoiceChannel voiceChannel)
    {
        await voiceChannel.DeleteAsync();
        await ReplyConfirmLocalizedAsync(strs.delvoich(Format.Bold(voiceChannel.Name)));
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.ManageChannels)]
    [BotPerm(GuildPerm.ManageChannels)]
    public async partial Task CreatVoiChanl([Leftover] string channelName)
    {
        var ch = await ctx.Guild.CreateVoiceChannelAsync(channelName);
        await ReplyConfirmLocalizedAsync(strs.createvoich(Format.Bold(ch.Name)));
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.ManageChannels)]
    [BotPerm(GuildPerm.ManageChannels)]
    public async partial Task DelTxtChanl([Leftover] ITextChannel toDelete)
    {
        await toDelete.DeleteAsync();
        await ReplyConfirmLocalizedAsync(strs.deltextchan(Format.Bold(toDelete.Name)));
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.ManageChannels)]
    [BotPerm(GuildPerm.ManageChannels)]
    public async partial Task CreaTxtChanl([Leftover] string channelName)
    {
        var txtCh = await ctx.Guild.CreateTextChannelAsync(channelName);
        await ReplyConfirmLocalizedAsync(strs.createtextchan(Format.Bold(txtCh.Name)));
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.ManageChannels)]
    [BotPerm(GuildPerm.ManageChannels)]
    public async partial Task SetTopic([Leftover] string topic = null)
    {
        var channel = (ITextChannel)ctx.Channel;
        topic ??= "";
        await channel.ModifyAsync(c => c.Topic = topic);
        await ReplyConfirmLocalizedAsync(strs.set_topic);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.ManageChannels)]
    [BotPerm(GuildPerm.ManageChannels)]
    public async partial Task SetChanlName([Leftover] string name)
    {
        var channel = (ITextChannel)ctx.Channel;
        await channel.ModifyAsync(c => c.Name = name);
        await ReplyConfirmLocalizedAsync(strs.set_channel_name);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.ManageChannels)]
    [BotPerm(GuildPerm.ManageChannels)]
    public async partial Task NsfwToggle()
    {
        var channel = (ITextChannel)ctx.Channel;
        var isEnabled = channel.IsNsfw;

        await channel.ModifyAsync(c => c.IsNsfw = !isEnabled);

        if (isEnabled)
            await ReplyConfirmLocalizedAsync(strs.nsfw_set_false);
        else
            await ReplyConfirmLocalizedAsync(strs.nsfw_set_true);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(ChannelPerm.ManageMessages)]
    [Priority(0)]
    public partial Task Edit(ulong messageId, [Leftover] string text)
        => Edit((ITextChannel)ctx.Channel, messageId, text);

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [Priority(1)]
    public async partial Task Edit(ITextChannel channel, ulong messageId, [Leftover] string text)
    {
        var userPerms = ((SocketGuildUser)ctx.User).GetPermissions(channel);
        var botPerms = ((SocketGuild)ctx.Guild).CurrentUser.GetPermissions(channel);
        if (!userPerms.Has(ChannelPermission.ManageMessages))
        {
            await ReplyErrorLocalizedAsync(strs.insuf_perms_u);
            return;
        }

        if (!botPerms.Has(ChannelPermission.ViewChannel))
        {
            await ReplyErrorLocalizedAsync(strs.insuf_perms_i);
            return;
        }

        await _service.EditMessage(ctx, channel, messageId, text);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(ChannelPerm.ManageMessages)]
    [BotPerm(ChannelPerm.ManageMessages)]
    public partial Task Delete(ulong messageId, StoopidTime time = null)
        => Delete((ITextChannel)ctx.Channel, messageId, time);

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task Delete(ITextChannel channel, ulong messageId, StoopidTime time = null)
        => await InternalMessageAction(channel, messageId, time, msg => msg.DeleteAsync());

    private async Task InternalMessageAction(
        ITextChannel channel,
        ulong messageId,
        StoopidTime time,
        Func<IMessage, Task> func)
    {
        var userPerms = ((SocketGuildUser)ctx.User).GetPermissions(channel);
        var botPerms = ((SocketGuild)ctx.Guild).CurrentUser.GetPermissions(channel);
        if (!userPerms.Has(ChannelPermission.ManageMessages))
        {
            await ReplyErrorLocalizedAsync(strs.insuf_perms_u);
            return;
        }

        if (!botPerms.Has(ChannelPermission.ManageMessages))
        {
            await ReplyErrorLocalizedAsync(strs.insuf_perms_i);
            return;
        }


        var msg = await channel.GetMessageAsync(messageId);
        if (msg is null)
        {
            await ReplyErrorLocalizedAsync(strs.msg_not_found);
            return;
        }

        if (time is null)
            await msg.DeleteAsync();
        else if (time.Time <= TimeSpan.FromDays(7))
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(time.Time);
                await msg.DeleteAsync();
            });
        }
        else
        {
            await ReplyErrorLocalizedAsync(strs.time_too_long);
            return;
        }

        await ctx.OkAsync();
    }
}