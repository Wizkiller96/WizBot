using Discord;
using Discord.Commands;
using WizBot.Extensions;
using System.Threading.Tasks;
using WizBot.Common.Attributes;
using WizBot.Modules.Administration.Services;
using WizBot.Core.Common;

namespace WizBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class SlowmodeCommands : WizBotSubmodule<SlowmodeService>
        {
            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [WizBotOptions(typeof(SlowmodeService.Options))]
            [Priority(1)]
            public Task Slowmode()
            {
                if (_service.StopSlowmode(Context.Channel.Id))
                {
                    return ReplyConfirmLocalized("slowmode_disabled");
                }
                else
                {
                    return Slowmode("-m", "1", "-s", "5");
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [WizBotOptions(typeof(SlowmodeService.Options))]
            [Priority(0)]
            public async Task Slowmode(params string[] args)
            {
                var (opts, succ) = OptionsParser.ParseFrom(new SlowmodeService.Options(), args);
                if (!succ)
                {
                    await ReplyErrorLocalized("invalid_params").ConfigureAwait(false);
                    return;
                }
                if (_service.StartSlowmode(Context.Channel.Id, opts.MessageCount, opts.PerSec))
                {
                    await Context.Channel.SendConfirmAsync(GetText("slowmode_init"),
                            GetText("slowmode_desc", Format.Bold(opts.MessageCount.ToString()), Format.Bold(opts.PerSec.ToString())))
                                                .ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [Priority(0)]
            public async Task SlowmodeWhitelist([Remainder]IGuildUser user)
            {
                bool added = _service.ToggleWhitelistUser(user.Guild.Id, user.Id);

                if (!added)
                    await ReplyConfirmLocalized("slowmodewl_user_stop", Format.Bold(user.ToString())).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("slowmodewl_user_start", Format.Bold(user.ToString())).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [Priority(1)]
            public async Task SlowmodeWhitelist([Remainder]IRole role)
            {
                bool added = _service.ToggleWhitelistRole(role.Guild.Id, role.Id);

                if (!added)
                    await ReplyConfirmLocalized("slowmodewl_role_stop", Format.Bold(role.ToString())).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("slowmodewl_role_start", Format.Bold(role.ToString())).ConfigureAwait(false);
            }
        }
    }
}