namespace WizBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class GreetCommands : WizBotModule<GreetService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task Boost()
        {
            var enabled = await _service.ToggleBoost(ctx.Guild.Id, ctx.Channel.Id);

            if (enabled)
                await Response().Confirm(strs.boost_on).SendAsync();
            else
                await Response().Pending(strs.boost_off).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task BoostDel(int timer = 30)
        {
            if (timer is < 0 or > 600)
                return;

            await _service.SetBoostDel(ctx.Guild.Id, timer);

            if (timer > 0)
                await Response().Confirm(strs.boostdel_on(timer)).SendAsync();
            else
                await Response().Pending(strs.boostdel_off).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task BoostMsg([Leftover] string? text = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                var boostMessage = _service.GetBoostMessage(ctx.Guild.Id);
                await Response().Confirm(strs.boostmsg_cur(boostMessage?.SanitizeMentions())).SendAsync();
                return;
            }

            var sendBoostEnabled = _service.SetBoostMessage(ctx.Guild.Id, ref text);

            await Response().Confirm(strs.boostmsg_new).SendAsync();
            if (!sendBoostEnabled)
                await Response().Pending(strs.boostmsg_enable($"`{prefix}boost`")).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task GreetDel(int timer = 30)
        {
            if (timer is < 0 or > 600)
                return;

            await _service.SetGreetDel(ctx.Guild.Id, timer);

            if (timer > 0)
                await Response().Confirm(strs.greetdel_on(timer)).SendAsync();
            else
                await Response().Pending(strs.greetdel_off).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task Greet()
        {
            var enabled = await _service.SetGreet(ctx.Guild.Id, ctx.Channel.Id);

            if (enabled)
                await Response().Confirm(strs.greet_on).SendAsync();
            else
                await Response().Pending(strs.greet_off).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task GreetMsg([Leftover] string? text = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                var greetMsg = _service.GetGreetMsg(ctx.Guild.Id);
                await Response().Confirm(strs.greetmsg_cur(greetMsg?.SanitizeMentions())).SendAsync();
                return;
            }

            var sendGreetEnabled = _service.SetGreetMessage(ctx.Guild.Id, ref text);

            await Response().Confirm(strs.greetmsg_new).SendAsync();

            if (!sendGreetEnabled)
                await Response().Pending(strs.greetmsg_enable($"`{prefix}greet`")).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task GreetDm()
        {
            var enabled = await _service.SetGreetDm(ctx.Guild.Id);

            if (enabled)
                await Response().Confirm(strs.greetdm_on).SendAsync();
            else
                await Response().Confirm(strs.greetdm_off).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task GreetDmMsg([Leftover] string? text = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                var dmGreetMsg = _service.GetDmGreetMsg(ctx.Guild.Id);
                await Response().Confirm(strs.greetdmmsg_cur(dmGreetMsg?.SanitizeMentions())).SendAsync();
                return;
            }

            var sendGreetEnabled = _service.SetGreetDmMessage(ctx.Guild.Id, ref text);

            await Response().Confirm(strs.greetdmmsg_new).SendAsync();
            if (!sendGreetEnabled)
                await Response().Pending(strs.greetdmmsg_enable($"`{prefix}greetdm`")).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task Bye()
        {
            var enabled = await _service.SetBye(ctx.Guild.Id, ctx.Channel.Id);

            if (enabled)
                await Response().Confirm(strs.bye_on).SendAsync();
            else
                await Response().Confirm(strs.bye_off).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ByeMsg([Leftover] string? text = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                var byeMsg = _service.GetByeMessage(ctx.Guild.Id);
                await Response().Confirm(strs.byemsg_cur(byeMsg?.SanitizeMentions())).SendAsync();
                return;
            }

            var sendByeEnabled = _service.SetByeMessage(ctx.Guild.Id, ref text);

            await Response().Confirm(strs.byemsg_new).SendAsync();
            if (!sendByeEnabled)
                await Response().Pending(strs.byemsg_enable($"`{prefix}bye`")).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ByeDel(int timer = 30)
        {
            await _service.SetByeDel(ctx.Guild.Id, timer);

            if (timer > 0)
                await Response().Confirm(strs.byedel_on(timer)).SendAsync();
            else
                await Response().Pending(strs.byedel_off).SendAsync();
        }


        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        [Ratelimit(5)]
        public async Task ByeTest([Leftover] IGuildUser? user = null)
        {
            user ??= (IGuildUser)ctx.User;

            await _service.ByeTest((ITextChannel)ctx.Channel, user);
            var enabled = _service.GetByeEnabled(ctx.Guild.Id);
            if (!enabled)
                await Response().Pending(strs.byemsg_enable($"`{prefix}bye`")).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        [Ratelimit(5)]
        public async Task GreetTest([Leftover] IGuildUser? user = null)
        {
            user ??= (IGuildUser)ctx.User;

            await _service.GreetTest((ITextChannel)ctx.Channel, user);
            var enabled = _service.GetGreetEnabled(ctx.Guild.Id);
            if (!enabled)
                await Response().Pending(strs.greetmsg_enable($"`{prefix}greet`")).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        [Ratelimit(5)]
        public async Task GreetDmTest([Leftover] IGuildUser? user = null)
        {
            user ??= (IGuildUser)ctx.User;

            var success = await _service.GreetDmTest(user);
            if (success)
                await ctx.OkAsync();
            else
                await ctx.WarningAsync();
            var enabled = _service.GetGreetDmEnabled(ctx.Guild.Id);
            if (!enabled)
                await Response().Pending(strs.greetdmmsg_enable($"`{prefix}greetdm`")).SendAsync();
        }
        
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        [Ratelimit(5)]
        public async Task BoostTest([Leftover] IGuildUser? user = null)
        {
            user ??= (IGuildUser)ctx.User;

            await _service.BoostTest((ITextChannel)ctx.Channel, user);
            var enabled = _service.GetBoostEnabled(ctx.Guild.Id);
            if (!enabled)
                await Response().Pending(strs.boostmsg_enable($"`{prefix}boost`")).SendAsync();
        }
    }
}