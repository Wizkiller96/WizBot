#nullable disable
using NadekoBot.Common.ModuleBehaviors;

namespace NadekoBot.Modules.Help.Services;

public class HelpService : IExecNoCommand, INService
{
    private readonly BotConfigService _bss;

    public HelpService(BotConfigService bss)
    {
        _bss = bss;
    }

    public Task ExecOnNoCommandAsync(IGuild guild, IUserMessage msg)
    {
        var settings = _bss.Data;
        if (guild is null)
        {
            if (string.IsNullOrWhiteSpace(settings.DmHelpText) || settings.DmHelpText == "-")
                return Task.CompletedTask;

            // only send dm help text if it contains one of the keywords, if they're specified
            // if they're not, then reply to every DM
            if (settings.DmHelpTextKeywords is not null &&
                !settings.DmHelpTextKeywords.Any(k => msg.Content.Contains(k)))
                return Task.CompletedTask;

            var rep = new ReplacementBuilder().WithOverride("%prefix%", () => _bss.Data.Prefix)
                .WithOverride("%bot.prefix%", () => _bss.Data.Prefix)
                .WithUser(msg.Author)
                .Build();

            var text = SmartText.CreateFrom(settings.DmHelpText);
            text = rep.Replace(text);

            return msg.Channel.SendAsync(text);
        }

        return Task.CompletedTask;
    }
}