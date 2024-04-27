using NadekoBot.Common.ModuleBehaviors;

namespace NadekoBot.Modules.Help.Services;

public class HelpService(BotConfigService bss, IReplacementService repSvc) : IExecNoCommand, INService
{
    public async Task ExecOnNoCommandAsync(IGuild? guild, IUserMessage msg)
    {
        var settings = bss.Data;
        if (guild is null)
        {
            if (string.IsNullOrWhiteSpace(settings.DmHelpText) || settings.DmHelpText == "-")
                return;

            // only send dm help text if it contains one of the keywords, if they're specified
            // if they're not, then reply to every DM
            if (settings.DmHelpTextKeywords is not null &&
                !settings.DmHelpTextKeywords.Any(k => msg.Content.Contains(k)))
            {
                return;
            }

            var repCtx = new ReplacementContext(guild: guild, channel: msg.Channel, users: msg.Author)
                .WithOverride("%prefix%", () => bss.Data.Prefix)
                .WithOverride("%bot.prefix%", () => bss.Data.Prefix);

            var text = SmartText.CreateFrom(settings.DmHelpText);
            text = await repSvc.ReplaceAsync(text, repCtx);

            await msg.Channel.SendAsync(text);
        }
    }
}