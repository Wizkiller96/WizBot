#nullable disable
using NadekoBot.Db;
using NadekoBot.Modules.Help.Services;

namespace NadekoBot.Modules.Utility.Services;

public class VerboseErrorsService : INService
{
    private readonly ConcurrentHashSet<ulong> _guildsEnabled;
    private readonly DbService _db;
    private readonly CommandHandler _ch;
    private readonly HelpService _hs;

    public VerboseErrorsService(
        Bot bot,
        DbService db,
        CommandHandler ch,
        HelpService hs)
    {
        _db = db;
        _ch = ch;
        _hs = hs;

        _ch.CommandErrored += LogVerboseError;

        _guildsEnabled = new(bot.AllGuildConfigs.Where(x => x.VerboseErrors).Select(x => x.GuildId));
    }

    private async Task LogVerboseError(CommandInfo cmd, ITextChannel channel, string reason)
    {
        if (channel is null || !_guildsEnabled.Contains(channel.GuildId))
            return;

        try
        {
            var embed = _hs.GetCommandHelp(cmd, channel.Guild)
                           .WithTitle("Command Error")
                           .WithDescription(reason)
                           .WithErrorColor();

            await channel.EmbedAsync(embed);
        }
        catch
        {
            //ignore
        }
    }

    public bool ToggleVerboseErrors(ulong guildId, bool? enabled = null)
    {
        using (var uow = _db.GetDbContext())
        {
            var gc = uow.GuildConfigsForId(guildId, set => set);

            if (enabled == null)
                enabled = gc.VerboseErrors = !gc.VerboseErrors; // Old behaviour, now behind a condition
            else
                gc.VerboseErrors = (bool)enabled; // New behaviour, just set it.

            uow.SaveChanges();
        }

        if ((bool)enabled) // This doesn't need to be duplicated inside the using block
            _guildsEnabled.Add(guildId);
        else
            _guildsEnabled.TryRemove(guildId);

        return (bool)enabled;
    }
}