using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using System;
using Discord.Commands;
using WizBot.Extensions;
using System.Linq;
using WizBot.Common.Attributes;
using WizBot.Common.ModuleBehaviors;
using WizBot.Core.Services;
using WizBot.Common;
using NLog;
using CommandLine;
using System.Collections.Generic;
using WizBot.Modules.Administration.Services;

namespace WizBot.Modules.Help.Services
{
    public class HelpService : ILateExecutor, INService
    {
        private readonly CommandHandler _ch;
        private readonly IBotStrings _strings;
        private readonly Logger _log;
        private readonly DiscordPermOverrideService _dpos;
        private readonly BotSettingsService _bss;

        public HelpService(CommandHandler ch, IBotStrings strings,
            DiscordPermOverrideService dpos, BotSettingsService bss)
        {
            _ch = ch;
            _strings = strings;
            _dpos = dpos;
            _bss = bss;
            _log = LogManager.GetCurrentClassLogger();
        }

        public Task LateExecute(DiscordSocketClient client, IGuild guild, IUserMessage msg)
        {
            try
            {
                var settings = _bss.Data;
                if (guild == null)
                {
                    if (string.IsNullOrWhiteSpace(settings.DmHelpText) || settings.DmHelpText == "-")
                        return Task.CompletedTask;

                    if (CREmbed.TryParse(settings.DmHelpText, out var embed))
                        return msg.Channel.EmbedAsync(embed);

                    return msg.Channel.SendMessageAsync(settings.DmHelpText);
                }
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
            }
            return Task.CompletedTask;
        }

        public EmbedBuilder GetCommandHelp(CommandInfo com, IGuild guild)
        {
            var prefix = _ch.GetPrefix(guild);

            var str = string.Format("**`{0}`**", prefix + com.Aliases.First());
            var alias = com.Aliases.Skip(1).FirstOrDefault();
            if (alias != null)
                str += string.Format(" **/ `{0}`**", prefix + alias);
            var em = new EmbedBuilder()
            .WithAuthor(eab => eab.WithName("WizBot - Command Helper")
                                          .WithUrl("http://wizbot.readthedocs.io/en/latest/")
                                          .WithIconUrl("http://i.imgur.com/fObUYFS.jpg"))
                .AddField(fb => fb.WithName(str)
                    .WithValue($"{com.RealSummary(_strings, prefix)}")
                    .WithIsInline(true));

            _dpos.TryGetOverrides(guild?.Id ?? 0, com.Name, out var overrides);
            var reqs = GetCommandRequirements(com, overrides);
            if (reqs.Any())
            {
                em.AddField(GetText("requires", guild),
                    string.Join("\n", reqs));
            }

            em
                .AddField(fb => fb.WithName(GetText("usage", guild))
                    .WithValue(string.Join("\n", Array.ConvertAll(com.RealRemarksArr(_strings, prefix),
                        arg => Format.Code(arg))))
                    .WithIsInline(false))
                .WithFooter(efb => efb.WithText(GetText("module", guild, com.Module.GetTopLevelModule().Name)))
                .WithColor(WizBot.OkColor);

            var opt = ((WizBotOptionsAttribute)com.Attributes.FirstOrDefault(x => x is WizBotOptionsAttribute))?.OptionType;
            if (opt != null)
            {
                var hs = GetCommandOptionHelp(opt);
                if (!string.IsNullOrWhiteSpace(hs))
                    em.AddField(GetText("options", guild), hs, false);
            }

            return em;
        }

        public static string GetCommandOptionHelp(Type opt)
        {
            var strs = GetCommandOptionHelpList(opt);

            return string.Join("\n", strs);
        }

        public static List<string> GetCommandOptionHelpList(Type opt)
        {
            var strs = opt.GetProperties()
                   .Select(x => x.GetCustomAttributes(true).FirstOrDefault(a => a is OptionAttribute))
                   .Where(x => x != null)
                   .Cast<OptionAttribute>()
                   .Select(x =>
                   {
                       var toReturn = $"`--{x.LongName}`";

                       if (!string.IsNullOrWhiteSpace(x.ShortName))
                           toReturn += $" (`-{x.ShortName}`)";

                       toReturn += $"   {x.HelpText}  ";
                       return toReturn;
                   })
                   .ToList();

            return strs;
        }


        public static string[] GetCommandRequirements(CommandInfo cmd, GuildPerm? overrides = null)
        {
            var toReturn = new List<string>();

            if (cmd.Preconditions.Any(x => x is OwnerOnlyAttribute))
                toReturn.Add("Bot Owner Only");

            if (cmd.Preconditions.Any(x => x is AdminOnlyAttribute))
                toReturn.Add("Bot Owner & Admin Only");

            var userPerm = (UserPermAttribute)cmd.Preconditions
                .FirstOrDefault(ca => ca is UserPermAttribute);

            string userPermString = string.Empty;
            if (!(userPerm is null))
            {
                if (userPerm.UserPermissionAttribute.ChannelPermission is ChannelPermission cPerm)
                    userPermString = GetPreconditionString((ChannelPerm)cPerm);
                if (userPerm.UserPermissionAttribute.GuildPermission is GuildPermission gPerm)
                    userPermString = GetPreconditionString((GuildPerm)gPerm);
            }

            if (overrides is null)
            {
                if (!string.IsNullOrWhiteSpace(userPermString))
                    toReturn.Add(userPermString);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(userPermString))
                    toReturn.Add(Format.Strikethrough(userPermString));

                toReturn.Add(GetPreconditionString(overrides.Value));
            }

            return toReturn.ToArray();
        }

        public static string GetPreconditionString(ChannelPerm perm)
        {
            return (perm.ToString() + " Channel Permission")
                .Replace("Guild", "Server", StringComparison.InvariantCulture);
        }

        public static string GetPreconditionString(GuildPerm perm)
        {
            return (perm.ToString() + " Server Permission")
                .Replace("Guild", "Server", StringComparison.InvariantCulture);
        }

        private string GetText(string text, IGuild guild, params object[] replacements) =>
            _strings.GetText(text, guild?.Id, replacements);
    }
}
