using Discord;
using Discord.Commands;
using WizBot.Common;
using WizBot.Common.Attributes;
using WizBot.Common.Replacements;
using Discord.WebSocket;
using WizBot.Core.Common;
using WizBot.Core.Modules.Help.Common;
using WizBot.Core.Services;
using WizBot.Extensions;
using WizBot.Modules.Help.Services;
using WizBot.Modules.Permissions.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WizBot.Modules.Help
{
    public class Help : WizBotTopLevelModule<HelpService>
    {
        public const string PatreonUrl = "https://patreon.com/WizNet";
        public const string PaypalUrl = "https://paypal.me/Wizkiller96Network";
        private readonly IBotCredentials _creds;
        private readonly CommandService _cmds;
        private readonly GlobalPermissionService _perms;
        private readonly IServiceProvider _services;
        private readonly DiscordSocketClient _client;

        public EmbedBuilder GetHelpStringEmbed()
        {
            var r = new ReplacementBuilder()
                .WithDefault(Context)
                .WithOverride("{0}", () => _creds.ClientId.ToString())
                .WithOverride("{1}", () => Prefix)
                .Build();


            if (!CREmbed.TryParse(Bc.BotConfig.HelpString, out var embed))
                return new EmbedBuilder().WithOkColor()
                    .WithDescription(String.Format(Bc.BotConfig.HelpString, _creds.ClientId, Prefix));

            r.Replace(embed);

            return embed.ToEmbed();
        }

        public Help(DiscordSocketClient client, IBotCredentials creds, GlobalPermissionService perms, CommandService cmds,
            IServiceProvider services)
        {
            _client = client;
            _creds = creds;
            _cmds = cmds;
            _perms = perms;
            _services = services;
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task Modules()
        {
            var embed = new EmbedBuilder().WithOkColor()
                .WithAuthor(eab => eab.WithIconUrl("http://i.imgur.com/fObUYFS.jpg"))
                .WithFooter(efb => efb.WithText("ℹ️" + GetText("modules_footer", Prefix)))
                .WithTitle(GetText("list_of_modules"))
                .WithDescription(string.Join("\n",
                                     _cmds.Modules.GroupBy(m => m.GetTopLevelModule())
                                         .Where(m => !_perms.BlockedModules.Contains(m.Key.Name.ToLowerInvariant()))
                                         .Select(m => "• " + m.Key.Name)
                                         .OrderBy(s => s)));
            await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [WizBotOptions(typeof(CommandsOptions))]
        public async Task Commands(string module = null, params string[] args)
        {
            var channel = ctx.Channel;

            var (opts, _) = OptionsParser.ParseFrom(new CommandsOptions(), args);

            module = module?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(module))
                return;

            // Find commands for that module
            // don't show commands which are blocked
            // order by name
            var cmds = _cmds.Commands.Where(c => c.Module.GetTopLevelModule().Name.ToUpperInvariant().StartsWith(module, StringComparison.InvariantCulture))
                                                .Where(c => !_perms.BlockedCommands.Contains(c.Aliases[0].ToLowerInvariant()))
                                                  .OrderBy(c => c.Aliases[0])
                                                  .Distinct(new CommandTextEqualityComparer());


            // check preconditions for all commands, but only if it's not 'all'
            // because all will show all commands anyway, no need to check
            HashSet<CommandInfo> succ = new HashSet<CommandInfo>();
            if (opts.View != CommandsOptions.ViewType.All)
            {
                succ = new HashSet<CommandInfo>((await Task.WhenAll(cmds.Select(async x =>
                {
                    var pre = (await x.CheckPreconditionsAsync(Context, _services).ConfigureAwait(false));
                    return (Cmd: x, Succ: pre.IsSuccess);
                })).ConfigureAwait(false))
                    .Where(x => x.Succ)
                    .Select(x => x.Cmd));

                if (opts.View == CommandsOptions.ViewType.Hide)
                {
                    // if hidden is specified, completely remove these commands from the list
                    cmds = cmds.Where(x => succ.Contains(x));
                }
            }

            var cmdsWithGroup = cmds.GroupBy(c => c.Module.Name.Replace("Commands", "", StringComparison.InvariantCulture))
                .OrderBy(x => x.Key == x.First().Module.Name ? int.MaxValue : x.Count());

            if (!cmds.Any())
            {
                if (opts.View != CommandsOptions.ViewType.Hide)
                    await ReplyErrorLocalizedAsync("module_not_found").ConfigureAwait(false);
                else
                    await ReplyErrorLocalizedAsync("module_not_found_or_cant_exec").ConfigureAwait(false);
                return;
            }
            var i = 0;
            var groups = cmdsWithGroup.GroupBy(x => i++ / 48).ToArray();
            var embed = new EmbedBuilder().WithOkColor();
            foreach (var g in groups)
            {
                var last = g.Count();
                for (i = 0; i < last; i++)
                {
                    var transformed = g.ElementAt(i).Select(x =>
                    {
                        //if cross is specified, and the command doesn't satisfy the requirements, cross it out
                        if (opts.View == CommandsOptions.ViewType.Cross)
                        {
                            return $"{(succ.Contains(x) ? "✅" : "❌")}{Prefix + x.Aliases.First(),-15} {"[" + x.Aliases.Skip(1).FirstOrDefault() + "]",-8}";
                        }
                        return $"{Prefix + x.Aliases.First(),-15} {"[" + x.Aliases.Skip(1).FirstOrDefault() + "]",-8}";
                    });

                    if (i == last - 1 && (i + 1) % 2 != 0)
                    {
                        var grp = 0;
                        var count = transformed.Count();
                        transformed = transformed
                            .GroupBy(x => grp++ % count / 2)
                            .Select(x =>
                            {
                                if (x.Count() == 1)
                                    return $"{x.First()}";
                                else
                                    return String.Concat(x);
                            });
                    }
                    embed.AddField(g.ElementAt(i).Key, "```css\n" + string.Join("\n", transformed) + "\n```", true);
                }
            }
            embed.WithFooter(GetText("commands_instr", Prefix));
            await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [Priority(0)]
        public async Task H([Leftover] string fail)
        {
            var prefixless = _cmds.Commands.FirstOrDefault(x => x.Aliases.Any(cmdName => cmdName.ToLowerInvariant() == fail));
            if (prefixless != null)
            {
                await H(prefixless).ConfigureAwait(false);
                return;
            }

            await ReplyErrorLocalizedAsync("command_not_found").ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [Priority(1)]
        public async Task H([Leftover] CommandInfo com = null)
        {
            var channel = ctx.Channel;

            if (com == null)
            {
                IMessageChannel ch = channel is ITextChannel
                    ? await ((IGuildUser)ctx.User).GetOrCreateDMChannelAsync().ConfigureAwait(false)
                    : channel;
                await ch.EmbedAsync(GetHelpStringEmbed()).ConfigureAwait(false);
                return;
            }

            var embed = _service.GetCommandHelp(com, ctx.Guild);
            await channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task Hgit()
        {
            Dictionary<string, List<object>> cmdData = new Dictionary<string, List<object>>();
            foreach (var com in _cmds.Commands.OrderBy(com => com.Module.GetTopLevelModule().Name).GroupBy(c => c.Aliases.First()).Select(g => g.First()))
            {
                var module = com.Module.GetTopLevelModule();
                string optHelpStr = null;
                var opt = ((WizBotOptionsAttribute)com.Attributes.FirstOrDefault(x => x is WizBotOptionsAttribute))?.OptionType;
                if (opt != null)
                {
                    optHelpStr = HelpService.GetCommandOptionHelp(opt);
                }
                var obj = new
                {
                    Aliases = com.Aliases.Select(x => Prefix + x).ToArray(),
                    Description = string.Format(com.Summary, Prefix),
                    Usage = JsonConvert.DeserializeObject<string[]>(com.Remarks).Select(x => string.Format(x, Prefix)).ToArray(),
                    Submodule = com.Module.Name,
                    Module = com.Module.GetTopLevelModule().Name,
                    Options = optHelpStr,
                    Requirements = HelpService.GetCommandRequirements(com),
                };
                if (cmdData.TryGetValue(module.Name, out var cmds))
                    cmds.Add(obj);
                else
                    cmdData.Add(module.Name, new List<object>
                    {
                        obj
                    });
            }
            File.WriteAllText("../../docs/cmds_new.json", JsonConvert.SerializeObject(cmdData, Formatting.Indented));
            await ReplyConfirmLocalizedAsync("commandlist_regen").ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task Guide()
        {
            await ConfirmLocalizedAsync("guide",
                "https://commands.wizbot.cf/",
                "http://ndocs.wizbot.cf/").ConfigureAwait(false);
        }

#if GLOBAL_WIZBOT

        [WizBotCommand, Usage, Description, Aliases]
        public async Task Report(string type, [Remainder] string message)
        {
            string[] rtypes = { "Bug", "Feedback", "bug", "feedback" };

            if (string.IsNullOrWhiteSpace(type))
                return;

            if (string.IsNullOrWhiteSpace(message))
                return;

            if (rtypes.Contains(type))
            {
                await _client.GetGuild(99273784988557312).GetTextChannel(566998481177280512).SendMessageAsync("<@99272781513920512>");
                await _client.GetGuild(99273784988557312).GetTextChannel(566998481177280512).EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithTitle($"New Bug/Feedback Report")
                    .WithThumbnailUrl($"{ctx.User.GetAvatarUrl()}")
                    .AddField(fb => fb.WithName("Reporter:").WithValue($"{ctx.User}").WithIsInline(true))
                    .AddField(fb => fb.WithName("Reporter ID:").WithValue($"{ctx.User.Id}").WithIsInline(true))
                    .AddField(fb => fb.WithName("Server Name:").WithValue($"{ctx.Guild.Name}").WithIsInline(true))
                    .AddField(fb => fb.WithName("Server ID:").WithValue($"{ctx.Guild.Id}").WithIsInline(true))
                    .AddField(fb => fb.WithName("Channel Name:").WithValue($"{ctx.Channel.Name}").WithIsInline(true))
                    .AddField(fb => fb.WithName("Channel ID:").WithValue($"{ctx.Channel.Id}").WithIsInline(true))
                    .AddField(fb => fb.WithName("Report Type:").WithValue(type).WithIsInline(false))
                    .AddField(fb => fb.WithName("Message:").WithValue($"{message}"))).ConfigureAwait(false);

                await ctx.Channel.SendConfirmAsync("🆗").ConfigureAwait(false);
            }
            else
                await ctx.Channel.EmbedAsync(new EmbedBuilder().WithErrorColor()
                    .WithTitle($"Error: Report not sent.")
                    .WithDescription("Please make sure you used the correct report types listed below.")
                    .AddField(fb => fb.WithName("Report Types:").WithValue("`Bug`, `Feedback`"))).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task AbuseReport(IGuildUser ruser, [Remainder] string rexplaination)
        {
            var user = ruser ?? ctx.User as IGuildUser;

            if (((user == null)) && (string.IsNullOrEmpty(rexplaination)))
            {
                await ctx.Channel.EmbedAsync(new EmbedBuilder().WithErrorColor()
                    .WithTitle($"Error: Abuse report not sent.")
                    .WithDescription("Please make sure you filled out all the fields correctly.")).ConfigureAwait(false);
            }
            else if (user == null)
            {
                await ctx.Channel.EmbedAsync(new EmbedBuilder().WithErrorColor()
                    .WithTitle($"Error: Abuse report not sent.")
                    .WithDescription("Please make sure you provided the username of the person you are reporting.")).ConfigureAwait(false);
            }
            else if (string.IsNullOrEmpty(rexplaination))
            {
                await ctx.Channel.EmbedAsync(new EmbedBuilder().WithErrorColor()
                    .WithTitle($"Error: Abuse report not sent.")
                    .WithDescription("Please make sure you provided and explaination in your report.")).ConfigureAwait(false);
            }
            else
                await _client.GetGuild(99273784988557312).GetTextChannel(590829242690961408).SendMessageAsync("<@&367646195889471499>");
                await _client.GetGuild(99273784988557312).GetTextChannel(590829242690961408).EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithTitle($"Abuse Report")
                    .WithThumbnailUrl($"{ctx.User.GetAvatarUrl()}")
                    .AddField(fb => fb.WithName("Reporter:").WithValue($"{ctx.User}").WithIsInline(true))
                    .AddField(fb => fb.WithName("Reporter ID:").WithValue($"{ctx.User.Id}").WithIsInline(true))
                    .AddField(fb => fb.WithName("Server Name:").WithValue($"{ctx.Guild.Name}").WithIsInline(true))
                    .AddField(fb => fb.WithName("Server ID:").WithValue($"{ctx.Guild.Id}").WithIsInline(true))
                    .AddField(fb => fb.WithName("Channel Name:").WithValue($"{ctx.Channel.Name}").WithIsInline(true))
                    .AddField(fb => fb.WithName("Channel ID:").WithValue($"{ctx.Channel.Id}").WithIsInline(true))
                    .AddField(fb => fb.WithName("Reported User:").WithValue($"**{user.Username}**#{user.Discriminator} | {user.Id.ToString()}").WithIsInline(false))
                    .AddField(fb => fb.WithName("Explaination/Proof:").WithValue($"{rexplaination}"))).ConfigureAwait(false);

                await ctx.Channel.SendConfirmAsync("Report sent to WizBot's Staff.").ConfigureAwait(false);
        }

#endif

        [WizBotCommand, Usage, Description, Aliases]
        public async Task Donate()
        {
            await ReplyConfirmLocalizedAsync("donate", PatreonUrl, PaypalUrl).ConfigureAwait(false);
        }

        private string GetRemarks(string[] arr)
        {
            return string.Join(" or ", arr.Select(x => Format.Code(x)));
        }
    }

    public class CommandTextEqualityComparer : IEqualityComparer<CommandInfo>
    {
        public bool Equals(CommandInfo x, CommandInfo y) => x.Aliases[0] == y.Aliases[0];

        public int GetHashCode(CommandInfo obj) => obj.Aliases[0].GetHashCode(StringComparison.InvariantCulture);

    }

    public class JsonCommandData
    {
        public string[] Aliases { get; set; }
        public string Description { get; set; }
        public string Usage { get; set; }
    }
}
