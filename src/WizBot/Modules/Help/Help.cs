using Discord.Commands;
using WizBot.Extensions;
using System.Linq;
using Discord;
using WizBot.Services;
using System.Threading.Tasks;
using WizBot.Attributes;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace WizBot.Modules.Help
{
    [WizBotModule("Help", "-")]
    public partial class Help : DiscordModule
    {
        private static string helpString { get; }
        public static string HelpString => String.Format(helpString, WizBot.Credentials.ClientId, WizBot.ModulePrefixes[typeof(Help).Name]);

        public static string DMHelpString { get; }

        static Help()
        {
            using (var uow = DbHandler.UnitOfWork())
            {
                var config = uow.BotConfig.GetOrCreate();
                helpString = config.HelpString;
                DMHelpString = config.DMHelpString;
            }
        }

        public Help() : base()
        {
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task Modules(IUserMessage umsg)
        {

            await umsg.Channel.SendMessageAsync("📜 **List of modules:** ```css\n• " + string.Join("\n• ", WizBot.CommandService.Modules.Select(m => m.Name)) + $"\n``` ℹ️ **Type** `-commands module_name` **to get a list of commands in that module.** ***e.g.*** `-commands games`")
                                       .ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task Commands(IUserMessage umsg, [Remainder] string module = null)
        {
            var channel = umsg.Channel;

            module = module?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(module))
                return;
            var cmds = WizBot.CommandService.Commands.Where(c => c.Module.Name.ToUpperInvariant().StartsWith(module))
                                                  .OrderBy(c => c.Text)
                                                  .Distinct(new CommandTextEqualityComparer())
                                                  .AsEnumerable();

            var cmdsArray = cmds as Command[] ?? cmds.ToArray();
            if (!cmdsArray.Any())
            {
                await channel.SendErrorAsync("That module does not exist.").ConfigureAwait(false);
                return;
            }
            if (module != "customreactions" && module != "conversations")
            {
                await channel.SendTableAsync("📃 **List Of Commands:**\n", cmdsArray, el => $"{el.Text,-15} {"["+el.Aliases.Skip(1).FirstOrDefault()+"]",-8}").ConfigureAwait(false);
            }
            else
            {
                await channel.SendMessageAsync("📃 **List Of Commands:**\n• " + string.Join("\n• ", cmdsArray.Select(c => $"{c.Text}")));
            }
            await channel.SendConfirmAsync($"ℹ️ **Type** `\"{WizBot.ModulePrefixes[typeof(Help).Name]}h CommandName\"` **to see the help for that specified command.** ***e.g.*** `-h >8ball`").ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task H(IUserMessage umsg, [Remainder] string comToFind = null)
        {
            var channel = umsg.Channel;

            comToFind = comToFind?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(comToFind))
            {
                IMessageChannel ch = channel is ITextChannel ? await ((IGuildUser)umsg.Author).CreateDMChannelAsync() : channel;
                await ch.SendMessageAsync(HelpString).ConfigureAwait(false);
                return;
            }
            var com = WizBot.CommandService.Commands.FirstOrDefault(c => c.Text.ToLowerInvariant() == comToFind || c.Aliases.Select(a=>a.ToLowerInvariant()).Contains(comToFind));

            if (com == null)
            {
                await channel.SendErrorAsync("I can't find that command. Please check the **command** and **command prefix** before trying again.");
                return;
            }
            var str = $"**`{com.Text}`**";
            var alias = com.Aliases.Skip(1).FirstOrDefault();
            if (alias != null)
                str += $" **/ `{alias}`**";
                var embed = new EmbedBuilder()
                .AddField(fb => fb.WithIndex(1).WithName(str).WithValue($"{ string.Format(com.Summary, com.Module.Prefix)} { GetCommandRequirements(com)}").WithIsInline(true))
                .AddField(fb => fb.WithIndex(2).WithName("**Usage**").WithValue($"{string.Format(com.Remarks, com.Module.Prefix)}").WithIsInline(false))
                .WithColor(WizBot.OkColor);
            await channel.EmbedAsync(embed.Build()).ConfigureAwait(false);
        }

        private string GetCommandRequirements(Command cmd)
        {
            return String.Join(" ", cmd.Source.CustomAttributes
                      .Where(ca => ca.AttributeType == typeof(OwnerOnlyAttribute) || ca.AttributeType == typeof(RequirePermissionAttribute))
                      .Select(ca =>
                      {
                          if (ca.AttributeType == typeof(OwnerOnlyAttribute))
                              return "**Bot Owner only.**";
                          else if (ca.AttributeType == typeof(RequirePermissionAttribute))
                              return $"**Requires {(GuildPermission)ca.ConstructorArguments.FirstOrDefault().Value} server permission.**".Replace("Guild", "Server");
                          else
                              return $"**Requires {(GuildPermission)ca.ConstructorArguments.FirstOrDefault().Value} channel permission.**".Replace("Guild", "Server");
                      }));
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public Task Hgit(IUserMessage umsg)
        {
            var helpstr = new StringBuilder();
            helpstr.AppendLine("You can support the project on paypal: <https://www.paypal.me/Wizkiller96Network>\n");
            helpstr.AppendLine("##Table Of Contents");
            helpstr.AppendLine(string.Join("\n", WizBot.CommandService.Modules.Where(m => m.Name.ToLowerInvariant() != "help").OrderBy(m => m.Name).Prepend(WizBot.CommandService.Modules.FirstOrDefault(m=>m.Name.ToLowerInvariant()=="help")).Select(m => $"- [{m.Name}](#{m.Name.ToLowerInvariant()})")));
            helpstr.AppendLine();
            string lastModule = null;
            foreach (var com in WizBot.CommandService.Commands.OrderBy(com=>com.Module.Name).GroupBy(c=>c.Text).Select(g=>g.First()))
            {
                if (com.Module.Name != lastModule)
                {
                    if (lastModule != null)
                    {
                        helpstr.AppendLine();
                        helpstr.AppendLine("###### [Back to TOC](#table-of-contents)");
                    }
                    helpstr.AppendLine();
                    helpstr.AppendLine("### " + com.Module.Name + "  ");
                    helpstr.AppendLine("Command and aliases | Description | Usage");
                    helpstr.AppendLine("----------------|--------------|-------");
                    lastModule = com.Module.Name;
                }
                helpstr.AppendLine($"`{com.Text}` {string.Join(" ", com.Aliases.Skip(1).Select(a=>"`"+a+"`"))} | {string.Format(com.Summary, com.Module.Prefix)} {GetCommandRequirements(com)} | {string.Format(com.Remarks, com.Module.Prefix)}");
            }
            helpstr = helpstr.Replace(WizBot.Client.GetCurrentUser().Username , "@BotName");
            File.WriteAllText("../../docs/Commands List.md", helpstr.ToString());
            return Task.CompletedTask;
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Guide(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;

            await channel.SendConfirmAsync(
@"**LIST OF COMMANDS**: Use `-commands [module name]`. You can see a list of modules by doing `-modules`
**Hosting Guides and docs can be found here**: <https://github.com/Wizkiller96/WizBot>").ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Donate(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;

            await channel.SendConfirmAsync(
$@"You can support the WizBot project on paypal. <https://www.paypal.me/Wizkiller96Network> or
You can send donations to `inick01@live.com`
Don't forget to leave your discord name or id in the message.

**Thank you** ♥️").ConfigureAwait(false);
        }
    }

    public class CommandTextEqualityComparer : IEqualityComparer<Command>
    {
        public bool Equals(Command x, Command y) => x.Text == y.Text;

        public int GetHashCode(Command obj) => obj.Text.GetHashCode();

    }
}
