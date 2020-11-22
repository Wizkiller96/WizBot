using Discord;
using Discord.Commands;
using WizBot.Common;
using WizBot.Common.Attributes;
using WizBot.Extensions;
using System;
using System.Text;
using System.Threading.Tasks;

namespace WizBot.Modules.Utility
{
    public partial class Utility
    {
        public class BotConfigCommands : WizBotSubmodule
        {
            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task BotConfigEdit()
            {
                var names = Enum.GetNames(typeof(BotConfigEditType));
                var valuesSb = new StringBuilder();
                foreach (var name in names)
                {
                    var value = Bc.GetValue(name);
                    if (name != "CurrencySign")
                        value = value.TrimTo(30);
                    valuesSb.AppendLine(value.Replace("\n", ""));
                }

                var embed = new EmbedBuilder()
                    .WithTitle("Bot Config")
                    .WithOkColor()
                    .AddField(fb => fb.WithName("Names").WithValue(string.Join("\n", names)).WithIsInline(true))
                    .AddField(fb => fb.WithName("Values").WithValue(valuesSb.ToString()).WithIsInline(true));
                await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task BotConfigEdit(BotConfigEditType type, [Leftover]string newValue = null)
            {
                if (string.IsNullOrWhiteSpace(newValue))
                    newValue = null;

                var success = Bc.Edit(type, newValue);

                if (!success)
                    await ReplyErrorLocalizedAsync("bot_config_edit_fail", Format.Bold(type.ToString()), Format.Bold(newValue ?? "NULL")).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalizedAsync("bot_config_edit_success", Format.Bold(type.ToString()), Format.Bold(newValue ?? "NULL")).ConfigureAwait(false);
            }
        }
    }
}
