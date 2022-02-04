#nullable disable
using NadekoBot.Modules.Gambling.Common;
using NadekoBot.Modules.Gambling.Services;

namespace NadekoBot.Modules.Gambling;

public partial class Gambling
{
    [Group]
    public partial class PlantPickCommands : GamblingSubmodule<PlantPickService>
    {
        private readonly ILogCommandService _logService;

        public PlantPickCommands(ILogCommandService logService, GamblingConfigService gss)
            : base(gss)
            => _logService = logService;

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async partial Task Pick(string pass = null)
        {
            if (!string.IsNullOrWhiteSpace(pass) && !pass.IsAlphaNumeric())
                return;

            var picked = await _service.PickAsync(ctx.Guild.Id, (ITextChannel)ctx.Channel, ctx.User.Id, pass);

            if (picked > 0)
            {
                var msg = await ReplyConfirmLocalizedAsync(strs.picked(N(picked)));
                msg.DeleteAfter(10);
            }

            if (((SocketGuild)ctx.Guild).CurrentUser.GuildPermissions.ManageMessages)
            {
                try
                {
                    _logService.AddDeleteIgnore(ctx.Message.Id);
                    await ctx.Message.DeleteAsync();
                }
                catch { }
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async partial Task Plant(ShmartNumber amount, string pass = null)
        {
            if (amount < 1)
                return;

            if (!string.IsNullOrWhiteSpace(pass) && !pass.IsAlphaNumeric())
                return;

            if (((SocketGuild)ctx.Guild).CurrentUser.GuildPermissions.ManageMessages)
            {
                _logService.AddDeleteIgnore(ctx.Message.Id);
                await ctx.Message.DeleteAsync();
            }

            var success = await _service.PlantAsync(ctx.Guild.Id,
                ctx.Channel,
                ctx.User.Id,
                ctx.User.ToString(),
                amount,
                pass);

            if (!success)
                await ReplyErrorLocalizedAsync(strs.not_enough(CurrencySign));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
#if GLOBAL_NADEKO
            [OwnerOnly]
#endif
        public async partial Task GenCurrency()
        {
            var enabled = _service.ToggleCurrencyGeneration(ctx.Guild.Id, ctx.Channel.Id);
            if (enabled)
                await ReplyConfirmLocalizedAsync(strs.curgen_enabled);
            else
                await ReplyConfirmLocalizedAsync(strs.curgen_disabled);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        [OwnerOnly]
        public partial Task GenCurList(int page = 1)
        {
            if (--page < 0)
                return Task.CompletedTask;
            var enabledIn = _service.GetAllGeneratingChannels();

            return ctx.SendPaginatedConfirmAsync(page,
                _ =>
                {
                    var items = enabledIn.Skip(page * 9).Take(9).ToList();

                    if (!items.Any())
                        return _eb.Create().WithErrorColor().WithDescription("-");

                    return items.Aggregate(_eb.Create().WithOkColor(),
                        (eb, i) => eb.AddField(i.GuildId.ToString(), i.ChannelId));
                },
                enabledIn.Count(),
                9);
        }
    }
}