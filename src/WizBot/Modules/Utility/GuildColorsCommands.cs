using SixLabors.ImageSharp.PixelFormats;

namespace WizBot.Modules.Utility;

public partial class Utility
{
    [Group("sclr")]
    public class GuildColorsCommands : WizBotModule<IGuildColorsService>
    {
        [Cmd]
        [UserPerm(GuildPerm.ManageGuild)]
        [RequireContext(ContextType.Guild)]
        public async Task ServerColorsShow()
        {
            EmbedBuilder[] ebs =
            [
                CreateEmbed()
                    .WithOkColor()
                    .WithDescription("\\✅"),
                CreateEmbed()
                    .WithPendingColor()
                    .WithDescription("\\⏳\\⚠️"),
                CreateEmbed()
                    .WithErrorColor()
                    .WithDescription("\\❌")
            ];

            await Response()
                  .Embeds(ebs)
                  .SendAsync();
        }

        [Cmd]
        [UserPerm(GuildPerm.ManageGuild)]
        [RequireContext(ContextType.Guild)]
        public async Task ServerColorOk([Leftover] Rgba32? color = null)
        {
            await _service.SetOkColor(ctx.Guild.Id, color);

            await Response().Confirm(strs.server_color_set).SendAsync();
            await ServerColorsShow();
        }

        [Cmd]
        [UserPerm(GuildPerm.ManageGuild)]
        [RequireContext(ContextType.Guild)]
        public async Task ServerColorPending([Leftover] Rgba32? color = null)
        {
            await _service.SetPendingColor(ctx.Guild.Id, color);

            await Response().Confirm(strs.server_color_set).SendAsync();
            await ServerColorsShow();
        }

        [Cmd]
        [UserPerm(GuildPerm.ManageGuild)]
        [RequireContext(ContextType.Guild)]
        public async Task ServerColorError([Leftover] Rgba32? color = null)
        {
            await _service.SetErrorColor(ctx.Guild.Id, color);

            await Response().Confirm(strs.server_color_set).SendAsync();
            await ServerColorsShow();
        }
    }
}