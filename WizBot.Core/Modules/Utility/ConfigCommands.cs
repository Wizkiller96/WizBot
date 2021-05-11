using Discord;
using Discord.Commands;
using WizBot.Common;
using WizBot.Common.Attributes;
using WizBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WizBot.Core.Services;
using WizBot.Modules.Administration.Services;

namespace WizBot.Modules.Utility
{
    public partial class Utility
    {
        public class ConfigCommands : WizBotSubmodule
        {
            private readonly BotSettingsService _bss;
            private readonly SelfService _selfService;

            private readonly IEnumerable<ISettingsService> _settingServices;

            public ConfigCommands(BotSettingsService bss, SelfService selfService, IEnumerable<ISettingsService> settingServices)
            {
                _settingServices = settingServices;
                _bss = bss;
                _selfService = selfService;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task BotConfigEdit()
            {
                var names = Enum.GetNames(typeof(BotConfigEditType))
                    .ToList();
                var valuesSb = new StringBuilder();
                foreach (var name in names)
                {
                    var value = Bc.GetValue(name);
                    if (name != "CurrencySign")
                        value = value.TrimTo(30);
                    valuesSb.AppendLine(value.Replace("\n", ""));
                }

                var propKeys = _bss.GetSettableProps();
                names.AddRange(propKeys);

                foreach (var key in propKeys)
                {
                    var value = _bss.GetSetting(key);
                    valuesSb.AppendLine(value?.TrimTo(30).Replace("\n", "") ?? "-");
                }

                var embed = new EmbedBuilder()
                    .WithTitle("Bot Config")
                    .WithOkColor()
                    .AddField(fb => fb.WithName("Names").WithValue(string.Join("\n", names)).WithIsInline(true))
                    .AddField(fb => fb.WithName("Values").WithValue(valuesSb.ToString()).WithIsInline(true));

                await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [Priority(1)]
            [OwnerOnly]
            public async Task BotConfigEdit(BotConfigEditType type, [Leftover] string newValue = null)
            {
                if (string.IsNullOrWhiteSpace(newValue))
                    newValue = null;

                var success = Bc.Edit(type, newValue);

                if (!success)
                    await ReplyErrorLocalizedAsync("config_edit_fail", Format.Bold(type.ToString()), Format.Bold(newValue ?? "NULL")).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalizedAsync("config_edit_success", Format.Bold(type.ToString()), Format.Bold(newValue ?? "NULL")).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [Priority(0)]
            [OwnerOnly]
            public async Task BotConfigEdit(string key, [Leftover] string newValue = null)
            {
                key = key.ToLowerInvariant();
                var props = _bss.GetSettableProps();
                if (!props.Contains(key))
                {
                    await ReplyErrorLocalizedAsync("setting_not_found");
                    return;
                }

                if (string.IsNullOrWhiteSpace(newValue))
                {
                    var val = _bss.GetSetting(key);
                    val = string.IsNullOrWhiteSpace(val)
                        ? "-"
                        : val;

                    var eb = new EmbedBuilder()
                        .WithTitle($"⚙️ {key}")
                        .WithDescription(Format.Sanitize(val))
                        .WithOkColor();

                    await Context.Channel.EmbedAsync(eb);
                    // print the value
                    return;
                }

                var success = _bss.SetSetting(key, newValue);

                if (!success)
                    await ReplyErrorLocalizedAsync("config_edit_fail", Format.Bold(key.ToString()), Format.Bold(newValue ?? "NULL")).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalizedAsync("config_edit_success", Format.Bold(key.ToString()), Format.Bold(newValue ?? "NULL")).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task BotConfigReload()
            {
                _bss.Reload();
                _selfService.ReloadBotConfig();
                await ReplyConfirmLocalizedAsync("config_reloaded", "Bot").ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task ConfigReload(string name)
            {
                var setting = _settingServices.FirstOrDefault(x =>
                    x.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase));

                if (setting is null)
                {
                    var configNames = _settingServices.Select(x => x.Name);
                    var embed = new EmbedBuilder()
                        .WithErrorColor()
                        .WithDescription(GetText("config_not_found", Format.Code(name)))
                        .AddField(GetText("config_list"), string.Join("\n", configNames));

                    await ctx.Channel.EmbedAsync(embed);
                    return;
                }

                setting.Reload();
                await ctx.OkAsync();
            }

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task Config(string name = null, string prop = null, string value = null)
            {
                var configNames = _settingServices.Select(x => x.Name);

                // if name is not provided, print available configs
                name = name?.ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(name))
                {
                    var embed = new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle(GetText("config_list"))
                        .WithDescription(string.Join("\n", configNames));

                    await ctx.Channel.EmbedAsync(embed);
                    return;
                }

                var setting = _settingServices.FirstOrDefault(x =>
                    x.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase));

                // if config name is not found, print error and the list of configs
                if (setting is null)
                {
                    var embed = new EmbedBuilder()
                        .WithErrorColor()
                        .WithDescription(GetText("config_not_found", Format.Code(name)))
                        .AddField(GetText("config_list"), string.Join("\n", configNames));

                    await ctx.Channel.EmbedAsync(embed);
                    return;
                }

                name = setting.Name;

                // if prop is not sent, then print the list of all props and values in that config
                prop = prop?.ToLowerInvariant();
                var propNames = setting.GetSettableProps();
                if (string.IsNullOrWhiteSpace(prop))
                {
                    var propStrings = GetPropsAndValuesString(setting, propNames);
                    var embed = new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle($"⚙️ {setting.Name}")
                        .WithDescription(propStrings);


                    await ctx.Channel.EmbedAsync(embed);
                    return;
                }
                // if the prop is invalid -> print error and list of 

                var exists = propNames.Any(x => x == prop);

                if (!exists)
                {
                    var propStrings = GetPropsAndValuesString(setting, propNames);
                    var propErrorEmbed = new EmbedBuilder()
                        .WithErrorColor()
                        .WithDescription(GetText("config_prop_not_found", Format.Code(prop), Format.Code(name)))
                        .AddField($"⚙️ {setting.Name}", propStrings);

                    await ctx.Channel.EmbedAsync(propErrorEmbed);
                    return;
                }

                // if prop is sent, but value is not, then we have to check
                // if prop is valid -> 
                if (string.IsNullOrWhiteSpace(value))
                {
                    value = setting.GetSetting(prop);
                    if (prop != "currency.sign")
                    {
                        Format.Code(Format.Sanitize(value?.TrimTo(1000)), "json");
                    }

                    if (string.IsNullOrWhiteSpace(value))
                        value = "-";

                    var embed = new EmbedBuilder()
                        .WithOkColor()
                        .AddField("Config", Format.Code(setting.Name), true)
                        .AddField("Prop", Format.Code(prop), true)
                        .AddField("Value", value);

                    var comment = setting.GetComment(prop);
                    if (!string.IsNullOrWhiteSpace(comment))
                        embed.AddField("Comment", comment);

                    await ctx.Channel.EmbedAsync(embed);
                    return;
                }

                var success = setting.SetSetting(prop, value);

                if (!success)
                {
                    await ReplyErrorLocalizedAsync("config_edit_fail", Format.Code(prop), Format.Code(value));
                    return;
                }

                await ctx.OkAsync();
            }

            private string GetPropsAndValuesString(ISettingsService setting, IEnumerable<string> names)
            {
                var propValues = names.Select(pr =>
                {
                    var val = setting.GetSetting(pr);
                    if (pr != "currency.sign")
                        val = val?.TrimTo(28);
                    return val?.Replace("\n", "") ?? "-";
                });

                var strings = names.Zip(propValues, (name, value) =>
                    $"{name,-25} = {value}\n");

                return Format.Code(string.Concat(strings), "hs");
            }
        }
    }
}