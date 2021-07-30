using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Administration.Services;
using NadekoBot.Modules.Gambling.Services;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Modules.Gambling.Common;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class PlantPickCommands : GamblingSubmodule<PlantPickService>
        {
            private readonly ILogCommandService logService;

            public PlantPickCommands(ILogCommandService logService, GamblingConfigService gss) : base(gss)
            {
                this.logService = logService;
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Pick(string pass = null)
            {
                if (!string.IsNullOrWhiteSpace(pass) && !pass.IsAlphaNumeric())
                {
                    return;
                }

                var picked = await _service.PickAsync(ctx.Guild.Id, (ITextChannel)ctx.Channel, ctx.User.Id, pass);

                if (picked > 0)
                {
                    var msg = await ReplyConfirmLocalizedAsync(strs.picked(picked + CurrencySign));
                    msg.DeleteAfter(10);
                }

                if (((SocketGuild)ctx.Guild).CurrentUser.GuildPermissions.ManageMessages)
                {
                    try
                    {
                        logService.AddDeleteIgnore(ctx.Message.Id);
                        await ctx.Message.DeleteAsync().ConfigureAwait(false);
                    }
                    catch { }
                }
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Plant(int amount = 1, string pass = null)
            {
                if (amount < 1)
                    return;

                if (!string.IsNullOrWhiteSpace(pass) && !pass.IsAlphaNumeric())
                {
                    return;
                }

                var success = await _service.PlantAsync(ctx.Guild.Id, ctx.Channel, ctx.User.Id, ctx.User.ToString(), amount, pass);
                if (!success)
                {
                    await ReplyErrorLocalizedAsync(strs.not_enough( CurrencySign));
                    return;
                }

                if (((SocketGuild)ctx.Guild).CurrentUser.GuildPermissions.ManageMessages)
                {
                    logService.AddDeleteIgnore(ctx.Message.Id);
                    await ctx.Message.DeleteAsync().ConfigureAwait(false);
                }
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
#if GLOBAL_NADEKO
            [OwnerOnly]
#endif
            public async Task GenCurrency()
            {
                bool enabled = _service.ToggleCurrencyGeneration(ctx.Guild.Id, ctx.Channel.Id);
                if (enabled)
                {
                    await ReplyConfirmLocalizedAsync(strs.curgen_enabled).ConfigureAwait(false);
                }
                else
                {
                    await ReplyConfirmLocalizedAsync(strs.curgen_disabled).ConfigureAwait(false);
                }
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            [OwnerOnly]
            public Task GenCurList(int page = 1)
            {
                if (--page < 0)
                    return Task.CompletedTask;
                var enabledIn = _service.GetAllGeneratingChannels();

                return ctx.SendPaginatedConfirmAsync(page, (cur) =>
                {
                    var items = enabledIn.Skip(page * 9).Take(9);

                    if (!items.Any())
                    {
                        return _eb.Create().WithErrorColor()
                            .WithDescription("-");
                    }

                    return items.Aggregate(_eb.Create().WithOkColor(),
                        (eb, i) => eb.AddField(i.GuildId.ToString(), i.ChannelId));
                }, enabledIn.Count(), 9);
            }
        }
    }
}
