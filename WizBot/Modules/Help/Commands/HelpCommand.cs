using Discord.Commands;
using WizBot.Extensions;
using WizBot.Modules;
using WizBot.Modules.Permissions.Classes;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WizBot.Classes.Help.Commands
{
    internal class HelpCommand : DiscordCommand
    {
        public Func<CommandEventArgs, Task> HelpFunc() => async e =>
        {
            var comToFind = e.GetArg("command")?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(comToFind))
            {
                await e.User.Send(HelpString).ConfigureAwait(false);
                return;
            }
            await Task.Run(async () =>
            {
                var com = WizBot.Client.GetService<CommandService>().AllCommands
                    .FirstOrDefault(c => c.Text.ToLowerInvariant().Equals(comToFind) ||
                                        c.Aliases.Select(a => a.ToLowerInvariant()).Contains(comToFind));
                if (com != null)
                    await e.Channel.SendMessage($"`Help for '{com.Text}':` {com.Description}").ConfigureAwait(false);
            }).ConfigureAwait(false);
        };
        public static string HelpString => (WizBot.IsBot
                                           ? $"To add me to your server, use this link** -> <https://discordapp.com/oauth2/authorize?client_id=170849867508350977&scope=bot&permissions=66186303>\n"
                                           : $"To invite me to your server, just send me an invite link here.") +
                                           $"You can use `{WizBot.Config.CommandPrefixes.Help}modules` command to see a list of all modules.\n" +
                                           $"You can use `{WizBot.Config.CommandPrefixes.Help}commands ModuleName`" +
                                           $" (for example `{WizBot.Config.CommandPrefixes.Help}commands Administration`) to see a list of all of the commands in that module.\n" +
                                           $"For a specific command help, use `{WizBot.Config.CommandPrefixes.Help}h \"Command name\"` (for example `-h \"!m q\"`)\n" +
                                           "**LIST OF COMMANDS CAN BE FOUND ON THIS LINK**\n\n <http://wizkiller96network.com/wizbot-cmds.html>";

        public static string DMHelpString => WizBot.Config.DMHelpString;

        public Action<CommandEventArgs> DoGitFunc() => e =>
        {
            string helpstr =
$@"######For more information, go to: **http://wizkiller96network.com/**
######You can donate on paypal: `inick01@live.com`
#WizBot List Of Commands  
Version: `{WizStats.Instance.BotVersion}`";


            string lastCategory = "";
            foreach (var com in WizBot.Client.GetService<CommandService>().AllCommands)
            {
                if (com.Category != lastCategory)
                {
                    helpstr += "\n### " + com.Category + "  \n";
                    helpstr += "Command and aliases | Description | Usage\n";
                    helpstr += "----------------|--------------|-------\n";
                    lastCategory = com.Category;
                }
                helpstr += PrintCommandHelp(com);
            }
            helpstr = helpstr.Replace(WizBot.BotMention, "@BotName");
            helpstr = helpstr.Replace("\n**Usage**:", " | ").Replace("**Usage**:", " | ").Replace("**Description:**", " | ").Replace("\n|", " |  \n");
#if DEBUG
            File.WriteAllText("../../../commandlist.md", helpstr);
#else
            File.WriteAllText("commandlist.md", helpstr);
#endif
        };

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "h")
                .Alias(Module.Prefix + "help", WizBot.BotMention + " help", WizBot.BotMention + " h", "~h")
                .Description("Either shows a help for a single command, or PMs you help link if no arguments are specified.\n**Usage**: '-h !m q' or just '-h' ")
                .Parameter("command", ParameterType.Unparsed)
                .Do(HelpFunc());
            cgb.CreateCommand(Module.Prefix + "hgit")
                .Description("Generates the commandlist.md file. **Owner Only!**")
                .AddCheck(SimpleCheckers.OwnerOnly())
                .Do(DoGitFunc());
            cgb.CreateCommand(Module.Prefix + "readme")
                .Alias(Module.Prefix + "guide")
                .Description("Sends a readme and a guide links to the channel.")
                .Do(async e =>
                    await e.Channel.SendMessage(
@"**FULL README**: <None>
**GUIDE ONLY**: <None>
**WINDOWS SETUP GUIDE**: <None>
**LINUX SETUP GUIDE**: <None>
**LIST OF COMMANDS**: <http://wizkiller96network.com/wizbot-cmds.html>").ConfigureAwait(false));

            cgb.CreateCommand(Module.Prefix + "donate")
                .Alias("~donate")
                .Description("Instructions for helping the project!")
                .Do(async e =>
                {
                    await e.Channel.SendMessage(
$@"I've created a **paypal** email for WizBot, so if you wish to support the project, you can send your donations to `inick01@live.com`
Don't forget to leave your discord name or id in the message, so that I can reward people who help out.
You can join WizBot server by typing {Module.Prefix}h and you will get an invite in a private message.
*If you want to support in some other way or on a different platform, please message me*"
                    ).ConfigureAwait(false);
                });
        }

        private static string PrintCommandHelp(Command com)
        {
            var str = "`" + com.Text + "`";
            str = com.Aliases.Aggregate(str, (current, a) => current + (", `" + a + "`"));
            str += " **Description:** " + com.Description + "\n";
            return str;
        }

        public HelpCommand(DiscordModule module) : base(module) { }
    }
}