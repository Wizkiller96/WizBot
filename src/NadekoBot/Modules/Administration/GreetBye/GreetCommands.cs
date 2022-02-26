namespace NadekoBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class GreetCommands : NadekoModule<GreetService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async partial Task Boost()
        {
            var enabled = await _service.ToggleBoost(ctx.Guild.Id, ctx.Channel.Id);

            if (enabled)
                await ReplyConfirmLocalizedAsync(strs.boost_on);
            else
                await ReplyPendingLocalizedAsync(strs.boost_off);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async partial Task BoostDel(int timer = 30)
        {
            if (timer is < 0 or > 600)
                return;

            await _service.SetBoostDel(ctx.Guild.Id, timer);

            if (timer > 0)
                await ReplyConfirmLocalizedAsync(strs.boostdel_on(timer));
            else
                await ReplyPendingLocalizedAsync(strs.boostdel_off);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async partial Task BoostMsg([Leftover] string? text = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                var boostMessage = _service.GetBoostMessage(ctx.Guild.Id);
                await ReplyConfirmLocalizedAsync(strs.boostmsg_cur(boostMessage?.SanitizeMentions()));
                return;
            }

            var sendBoostEnabled = _service.SetBoostMessage(ctx.Guild.Id, ref text);

            await ReplyConfirmLocalizedAsync(strs.boostmsg_new);
            if (!sendBoostEnabled)
                await ReplyPendingLocalizedAsync(strs.boostmsg_enable($"`{prefix}boost`"));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async partial Task GreetDel(int timer = 30)
        {
            if (timer is < 0 or > 600)
                return;

            await _service.SetGreetDel(ctx.Guild.Id, timer);

            if (timer > 0)
                await ReplyConfirmLocalizedAsync(strs.greetdel_on(timer));
            else
                await ReplyPendingLocalizedAsync(strs.greetdel_off);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async partial Task Greet()
        {
            var enabled = await _service.SetGreet(ctx.Guild.Id, ctx.Channel.Id);

            if (enabled)
                await ReplyConfirmLocalizedAsync(strs.greet_on);
            else
                await ReplyPendingLocalizedAsync(strs.greet_off);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async partial Task GreetMsg([Leftover] string? text = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                var greetMsg = _service.GetGreetMsg(ctx.Guild.Id);
                await ReplyConfirmLocalizedAsync(strs.greetmsg_cur(greetMsg?.SanitizeMentions()));
                return;
            }

            var sendGreetEnabled = _service.SetGreetMessage(ctx.Guild.Id, ref text);

            await ReplyConfirmLocalizedAsync(strs.greetmsg_new);

            if (!sendGreetEnabled)
                await ReplyPendingLocalizedAsync(strs.greetmsg_enable($"`{prefix}greet`"));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async partial Task GreetDm()
        {
            var enabled = await _service.SetGreetDm(ctx.Guild.Id);

            if (enabled)
                await ReplyConfirmLocalizedAsync(strs.greetdm_on);
            else
                await ReplyConfirmLocalizedAsync(strs.greetdm_off);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async partial Task GreetDmMsg([Leftover] string? text = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                var dmGreetMsg = _service.GetDmGreetMsg(ctx.Guild.Id);
                await ReplyConfirmLocalizedAsync(strs.greetdmmsg_cur(dmGreetMsg?.SanitizeMentions()));
                return;
            }

            var sendGreetEnabled = _service.SetGreetDmMessage(ctx.Guild.Id, ref text);

            await ReplyConfirmLocalizedAsync(strs.greetdmmsg_new);
            if (!sendGreetEnabled)
                await ReplyPendingLocalizedAsync(strs.greetdmmsg_enable($"`{prefix}greetdm`"));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async partial Task Bye()
        {
            var enabled = await _service.SetBye(ctx.Guild.Id, ctx.Channel.Id);

            if (enabled)
                await ReplyConfirmLocalizedAsync(strs.bye_on);
            else
                await ReplyConfirmLocalizedAsync(strs.bye_off);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async partial Task ByeMsg([Leftover] string? text = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                var byeMsg = _service.GetByeMessage(ctx.Guild.Id);
                await ReplyConfirmLocalizedAsync(strs.byemsg_cur(byeMsg?.SanitizeMentions()));
                return;
            }

            var sendByeEnabled = _service.SetByeMessage(ctx.Guild.Id, ref text);

            await ReplyConfirmLocalizedAsync(strs.byemsg_new);
            if (!sendByeEnabled)
                await ReplyPendingLocalizedAsync(strs.byemsg_enable($"`{prefix}bye`"));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async partial Task ByeDel(int timer = 30)
        {
            await _service.SetByeDel(ctx.Guild.Id, timer);

            if (timer > 0)
                await ReplyConfirmLocalizedAsync(strs.byedel_on(timer));
            else
                await ReplyPendingLocalizedAsync(strs.byedel_off);
        }


        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        [Ratelimit(5)]
        public async partial Task ByeTest([Leftover] IGuildUser? user = null)
        {
            user ??= (IGuildUser)ctx.User;

            await _service.ByeTest((ITextChannel)ctx.Channel, user);
            var enabled = _service.GetByeEnabled(ctx.Guild.Id);
            if (!enabled)
                await ReplyPendingLocalizedAsync(strs.byemsg_enable($"`{prefix}bye`"));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        [Ratelimit(5)]
        public async partial Task GreetTest([Leftover] IGuildUser? user = null)
        {
            user ??= (IGuildUser)ctx.User;

            await _service.GreetTest((ITextChannel)ctx.Channel, user);
            var enabled = _service.GetGreetEnabled(ctx.Guild.Id);
            if (!enabled)
                await ReplyPendingLocalizedAsync(strs.greetmsg_enable($"`{prefix}greet`"));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        [Ratelimit(5)]
        public async partial Task GreetDmTest([Leftover] IGuildUser? user = null)
        {
            user ??= (IGuildUser)ctx.User;

            var success = await _service.GreetDmTest(user);
            if (success)
                await ctx.OkAsync();
            else
                await ctx.WarningAsync();
            var enabled = _service.GetGreetDmEnabled(ctx.Guild.Id);
            if (!enabled)
                await ReplyPendingLocalizedAsync(strs.greetdmmsg_enable($"`{prefix}greetdm`"));
        }
    }
}