using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using NadekoBot.Services;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class ServerGreetCommands : NadekoSubmodule<GreetSettingsService>
        {
            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageGuild)]
            public async Task Boost()
            {
                var enabled = await _service.ToggleBoost(ctx.Guild.Id, ctx.Channel.Id);

                if (enabled)
                    await ReplyConfirmLocalizedAsync(strs.boost_on).ConfigureAwait(false);
                else
                    await ReplyPendingLocalizedAsync(strs.boost_off).ConfigureAwait(false);
            }
            
            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageGuild)]
            public async Task BoostDel(int timer = 30)
            {
                if (timer < 0 || timer > 600)
                    return;

                await _service.SetBoostDel(ctx.Guild.Id, timer);

                if (timer > 0)
                    await ReplyConfirmLocalizedAsync(strs.boostdel_on(timer));
                else
                    await ReplyPendingLocalizedAsync(strs.boostdel_off).ConfigureAwait(false);
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageGuild)]
            public Task BoostMsg()
            {
                var boostMessage = _service.GetBoostMessage(ctx.Guild.Id);
                return ReplyConfirmLocalizedAsync(strs.boostmsg_cur(boostMessage?.SanitizeMentions()));
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageGuild)]
            public async Task BoostMsg([Leftover] string text)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    await BoostMsg().ConfigureAwait(false);
                    return;
                }

                var sendBoostEnabled = _service.SetBoostMessage(ctx.Guild.Id, ref text);

                await ReplyConfirmLocalizedAsync(strs.boostmsg_new).ConfigureAwait(false);
                if (!sendBoostEnabled)
                    await ReplyPendingLocalizedAsync(strs.boostmsg_enable($"`{Prefix}boost`"));
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageGuild)]
            public async Task GreetDel(int timer = 30)
            {
                if (timer < 0 || timer > 600)
                    return;

                await _service.SetGreetDel(ctx.Guild.Id, timer).ConfigureAwait(false);

                if (timer > 0)
                    await ReplyConfirmLocalizedAsync(strs.greetdel_on(timer));
                else
                    await ReplyPendingLocalizedAsync(strs.greetdel_off).ConfigureAwait(false);
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageGuild)]
            public async Task Greet()
            {
                var enabled = await _service.SetGreet(ctx.Guild.Id, ctx.Channel.Id).ConfigureAwait(false);

                if (enabled)
                    await ReplyConfirmLocalizedAsync(strs.greet_on).ConfigureAwait(false);
                else
                    await ReplyPendingLocalizedAsync(strs.greet_off).ConfigureAwait(false);
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageGuild)]
            public Task GreetMsg()
            {
                var greetMsg = _service.GetGreetMsg(ctx.Guild.Id);
                return ReplyConfirmLocalizedAsync(strs.greetmsg_cur(greetMsg?.SanitizeMentions()));
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageGuild)]
            public async Task GreetMsg([Leftover] string text)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    await GreetMsg().ConfigureAwait(false);
                    return;                    
                }

                var sendGreetEnabled = _service.SetGreetMessage(ctx.Guild.Id, ref text);

                await ReplyConfirmLocalizedAsync(strs.greetmsg_new).ConfigureAwait(false);
                if (!sendGreetEnabled)
                    await ReplyPendingLocalizedAsync(strs.greetmsg_enable($"`{Prefix}greet`"));
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageGuild)]
            public async Task GreetDm()
            {
                var enabled = await _service.SetGreetDm(ctx.Guild.Id).ConfigureAwait(false);

                if (enabled)
                    await ReplyConfirmLocalizedAsync(strs.greetdm_on).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalizedAsync(strs.greetdm_off).ConfigureAwait(false);
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageGuild)]
            public Task GreetDmMsg()
            {
                var dmGreetMsg = _service.GetDmGreetMsg(ctx.Guild.Id);
                return ReplyConfirmLocalizedAsync(strs.greetdmmsg_cur(dmGreetMsg?.SanitizeMentions()));
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageGuild)]
            public async Task GreetDmMsg([Leftover] string text = null)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    await GreetDmMsg().ConfigureAwait(false);
                    return;
                }

                var sendGreetEnabled = _service.SetGreetDmMessage(ctx.Guild.Id, ref text);

                await ReplyConfirmLocalizedAsync(strs.greetdmmsg_new).ConfigureAwait(false);
                if (!sendGreetEnabled)
                    await ReplyPendingLocalizedAsync(strs.greetdmmsg_enable($"`{Prefix}greetdm`"));
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageGuild)]
            public async Task Bye()
            {
                var enabled = await _service.SetBye(ctx.Guild.Id, ctx.Channel.Id).ConfigureAwait(false);

                if (enabled)
                    await ReplyConfirmLocalizedAsync(strs.bye_on).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalizedAsync(strs.bye_off).ConfigureAwait(false);
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageGuild)]
            public Task ByeMsg()
            {
                var byeMsg = _service.GetByeMessage(ctx.Guild.Id);
                return ReplyConfirmLocalizedAsync(strs.byemsg_cur(byeMsg?.SanitizeMentions()));
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageGuild)]
            public async Task ByeMsg([Leftover] string text)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    await ByeMsg().ConfigureAwait(false);
                    return;
                }

                var sendByeEnabled = _service.SetByeMessage(ctx.Guild.Id, ref text);

                await ReplyConfirmLocalizedAsync(strs.byemsg_new).ConfigureAwait(false);
                if (!sendByeEnabled)
                    await ReplyPendingLocalizedAsync(strs.byemsg_enable($"`{Prefix}bye`"));
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageGuild)]
            public async Task ByeDel(int timer = 30)
            {
                await _service.SetByeDel(ctx.Guild.Id, timer).ConfigureAwait(false);

                if (timer > 0)
                    await ReplyConfirmLocalizedAsync(strs.byedel_on(timer));
                else
                    await ReplyPendingLocalizedAsync(strs.byedel_off).ConfigureAwait(false);
            }


            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageGuild)]
            [Ratelimit(5)]
            public async Task ByeTest([Leftover] IGuildUser user = null)
            {
                user = user ?? (IGuildUser) ctx.User;
                
                await _service.ByeTest((ITextChannel)ctx.Channel, user);
                var enabled = _service.GetByeEnabled(ctx.Guild.Id);
                if (!enabled)
                {
                    await ReplyPendingLocalizedAsync(strs.byemsg_enable($"`{Prefix}bye`"));
                }
            }
            
            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageGuild)]
            [Ratelimit(5)]
            public async Task GreetTest([Leftover] IGuildUser user = null)
            {
                user = user ?? (IGuildUser) ctx.User;
                
                await _service.GreetTest((ITextChannel)ctx.Channel, user);
                var enabled = _service.GetGreetEnabled(ctx.Guild.Id);
                if (!enabled)
                {
                    await ReplyPendingLocalizedAsync(strs.greetmsg_enable($"`{Prefix}greet`"));
                }
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageGuild)]
            [Ratelimit(5)]
            public async Task GreetDmTest([Leftover] IGuildUser user = null)
            {
                user = user ?? (IGuildUser) ctx.User;
                
                var channel = await user.GetOrCreateDMChannelAsync();
                var success = await _service.GreetDmTest(channel, user);
                if (success)
                    await ctx.OkAsync();
                else
                    await ctx.WarningAsync();
                var enabled = _service.GetGreetDmEnabled(ctx.Guild.Id);
                if (!enabled)
                    await ReplyPendingLocalizedAsync(strs.greetdmmsg_enable($"`{Prefix}greetdm`"));
            }
        }
    }
}