using Discord.Modules;
using WizBot.Extensions;
using WizBot.Modules.Permissions.Classes;
using WizBot.Modules.Programming.Commands;

namespace WizBot.Modules.Programming
{
    class ProgrammingModule : DiscordModule
    {
        public override string Prefix => WizBot.Config.CommandPrefixes.Programming;

        public ProgrammingModule()
        {
            commands.Add(new HaskellRepl(this));
        }

        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands("", cgb =>
            {
                cgb.AddCheck(PermissionChecker.Instance);
                commands.ForEach(c => c.Init(cgb));
            });
        }
    }
}