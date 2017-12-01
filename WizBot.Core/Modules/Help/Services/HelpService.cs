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
using WizBot.Core.Services.Impl;
using WizBot.Common;
using NLog;
using CommandLine;
using CommandLine.Text;

namespace WizBot.Modules.Help.Services
{
    public class HelpService : ILateExecutor, INService
    {
        private readonly IBotConfigProvider _bc;
        private readonly CommandHandler _ch;
        private readonly WizBotStrings _strings;
        private readonly Logger _log;

        public HelpService(IBotConfigProvider bc, CommandHandler ch, WizBotStrings strings)
        {
            _bc = bc;
            _ch = ch;
            _strings = strings;
            _log = LogManager.GetCurrentClassLogger();
        }

        public Task LateExecute(DiscordSocketClient client, IGuild guild, IUserMessage msg)
        {
            try
            {
                if (guild == null)
                {
                    if (CREmbed.TryParse(_bc.BotConfig.DMHelpString, out var embed))
                        return msg.Channel.EmbedAsync(embed.ToEmbed(), embed.PlainText?.SanitizeMentions() ?? "");

                    return msg.Channel.SendMessageAsync(_bc.BotConfig.DMHelpString);
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
                .AddField(fb => fb.WithName(str).WithValue($"{com.RealSummary(prefix)} {GetCommandRequirements(com, guild)}").WithIsInline(true))
                .AddField(fb => fb.WithName(GetText("usage", guild)).WithValue(com.RealRemarks(prefix)).WithIsInline(false))
                .WithFooter(efb => efb.WithText(GetText("module", guild, com.Module.GetTopLevelModule().Name)))
                .WithColor(WizBot.OkColor);

            var opt = (WizBotOptions)com.Attributes.FirstOrDefault(x => x is WizBotOptions);
            if (opt != null)
            {
                var x = Activator.CreateInstance(opt.OptionType);
                var res = Parser.Default.ParseArguments(new[] { "--help" }, x.GetType());
                var hs = HelpText.RenderUsageTextAsLines();
                if (!string.IsNullOrWhiteSpace(hs))
                    em.AddField(GetText("options", guild), hs, false);
            }
 
            return em;
        }

        public string GetCommandRequirements(CommandInfo cmd, IGuild guild) =>
            string.Join(" ", cmd.Preconditions
                  .Where(ca => ca is OwnerOnlyAttribute || ca is AdminOnlyAttribute || ca is RequireUserPermissionAttribute)
                  .Select(ca =>
                  {
                      if (ca is OwnerOnlyAttribute)
                          return Format.Bold(GetText("bot_owner_only", guild));
                      if (ca is AdminOnlyAttribute)
                          return Format.Bold(GetText("bot_admin_only", guild));
                      var cau = (RequireUserPermissionAttribute)ca;
                      if (cau.GuildPermission != null)
                          return Format.Bold(GetText("server_permission", guild, cau.GuildPermission))
                                       .Replace("Guild", "Server");
                      return Format.Bold(GetText("channel_permission", guild, cau.ChannelPermission))
                                       .Replace("Guild", "Server");
                  }));

        private string GetText(string text, IGuild guild, params object[] replacements) =>
            _strings.GetText(text, guild?.Id, "Help".ToLowerInvariant(), replacements);
    }
}