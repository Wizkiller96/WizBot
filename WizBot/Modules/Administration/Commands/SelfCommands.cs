using Discord.Commands;
using WizBot.Classes;
using WizBot.Modules.Permissions.Classes;
using System.Linq;

namespace WizBot.Modules.Administration.Commands
{
    class SelfCommands : DiscordCommand
    {
        public SelfCommands(DiscordModule module) : base(module)
        {
        }

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "leave")
                .Description($"Makes WizBot leave the server. Either name or id required. **Bot Owner Only!**| `{Prefix}leave 123123123331`")
                .Parameter("arg", ParameterType.Required)
                .AddCheck(SimpleCheckers.OwnerOnly())
                .Do(async e =>
                {
                    var arg = e.GetArg("arg").Trim();
                    var server = WizBot.Client.Servers.FirstOrDefault(s => s.Id.ToString() == arg) ??
                                 WizBot.Client.FindServers(arg).FirstOrDefault();
                    if (server == null)
                    {
                        await e.Channel.SendMessage("Cannot find that server").ConfigureAwait(false);
                        return;
                    }
                    if (!server.IsOwner)
                    {
                        await server.Leave().ConfigureAwait(false);
                    }
                    else
                    {
                        await server.Delete().ConfigureAwait(false);
                    }
                    await WizBot.SendMessageToOwner("Left server " + server.Name).ConfigureAwait(false);
                });
        }
    }
}
