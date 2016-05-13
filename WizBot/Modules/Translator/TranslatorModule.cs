using Discord.Modules;
using WizBot.Extensions;
using WizBot.Modules.Permissions.Classes;

namespace WizBot.Modules.Translator
{
    internal class TranslatorModule : DiscordModule
    {
        public TranslatorModule()
        {
            commands.Add(new TranslateCommand(this));
            commands.Add(new ValidLanguagesCommand(this));
        }

        public override string Prefix { get; } = WizBot.Config.CommandPrefixes.Searches;

        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands("", cgb =>
            {
                cgb.AddCheck(PermissionChecker.Instance);
                commands.ForEach(cmd => cmd.Init(cgb));
            });
        }

    }
}
