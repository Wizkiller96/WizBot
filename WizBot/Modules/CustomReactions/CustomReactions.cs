using Discord.Commands;
using Discord.Modules;
using WizBot.Extensions;
using WizBot.Modules.Permissions.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WizBot.Modules.CustomReactions
{
    internal class CustomReactionsModule : DiscordModule
    {
        public override string Prefix { get; } = "";

        private Random rng = new Random();

        private Dictionary<Regex, Func<CommandEventArgs, Match, string>> commandFuncs;

        public CustomReactionsModule()
        {
            commandFuncs = new Dictionary<Regex, Func<CommandEventArgs, Match, string>>
                 {
                    {new Regex(@"(?:%rng%|%rng:(\d{1,9})-(\d{1,9})%)"), (e,m) => {
                        int start, end;
                        if (m.Groups[1].Success)
                        {
                            start = int.Parse(m.Groups[1].Value);
                            end = int.Parse(m.Groups[2].Value);
                            return rng.Next(start, end).ToString();
                        }else return rng.Next().ToString();
                        } },
                    {new Regex("%mention%"), (e,m) => WizBot.BotMention },
                    {new Regex("%user%"), (e,m) => e.User.Mention },
                    {new Regex("%target%"), (e,m) => e.GetArg("args")?.Trim() ?? "" },

                 };
        }

        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands("", cgb =>
             {
                 cgb.AddCheck(PermissionChecker.Instance);

                 foreach (var command in WizBot.Config.CustomReactions)
                 {
                     var commandName = command.Key.Replace("%mention%", WizBot.BotMention);

                     var c = cgb.CreateCommand(commandName);
                     if (commandName.Contains(WizBot.BotMention))
                         c.Alias(commandName.Replace("<@", "<@!"));
                     c.Description($"Custom reaction. | `{command.Key}`")
                         .Parameter("args", ParameterType.Unparsed)
                         .Do(async e =>
                          {
                              string str = command.Value[rng.Next(0, command.Value.Count())];
                              commandFuncs.Keys.ForEach(key => str = key.Replace(str, m => commandFuncs[key](e, m)));


                              await e.Channel.SendMessage(str).ConfigureAwait(false);
                          });
                 }
             });
        }
    }
}
