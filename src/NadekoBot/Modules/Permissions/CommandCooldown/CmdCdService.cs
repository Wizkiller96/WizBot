#nullable disable
using NadekoBot.Common.ModuleBehaviors;

namespace NadekoBot.Modules.Permissions.Services;

public sealed class CmdCdService : IExecPreCommand, IReadyExecutor, INService
{
    private readonly ConcurrentDictionary<ulong, ConcurrentDictionary<string, int>> _settings = new();

    private readonly ConcurrentDictionary<(ulong, string), ConcurrentDictionary<ulong, DateTime>> _activeCooldowns =
        new();

    public int Priority => 0;

    public CmdCdService(Bot bot)
    {
        _settings = bot
            .AllGuildConfigs
            .ToDictionary(x => x.GuildId, x => x.CommandCooldowns
                .ToDictionary(c => c.CommandName, c => c.Seconds)
                .ToConcurrent())
            .ToConcurrent();
    }

    public Task<bool> ExecPreCommandAsync(ICommandContext context, string moduleName, CommandInfo command)
        => TryBlock(context.Guild, context.User, command.Name.ToLowerInvariant());

    public async Task<bool> TryBlock(IGuild guild, IUser user, string commandName)
    {
        if (!_settings.TryGetValue(guild.Id, out var cooldownSettings))
            return false;

        if (!cooldownSettings.TryGetValue(commandName, out var cdSeconds))
            return false;

        var cooldowns = _activeCooldowns.GetOrAdd(
            (guild.Id, commandName),
            static _ => new());

        // if user is not already on cooldown, add 
        if (cooldowns.TryAdd(user.Id, DateTime.UtcNow))
        {
            return false;
        }

        // if there is an entry, maybe it expired. Try to check if it expired and don't fail if it did
        // - just update
        if (cooldowns.TryGetValue(user.Id, out var oldValue))
        {
            var diff = DateTime.UtcNow - oldValue;
            if (diff.Seconds > cdSeconds)
            {
                if (cooldowns.TryUpdate(user.Id, DateTime.UtcNow, oldValue))
                    return false;
            }
        }

        return true;
    }

    public async Task OnReadyAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        while (await timer.WaitForNextTickAsync())
        {
            var now = DateTime.UtcNow;
            // once per hour delete expired entries
            foreach (var ((guildId, commandName), dict) in _activeCooldowns)
            {
                // if this pair no longer has associated config, that means it has been removed.
                // remove all cooldowns
                if (!_settings.TryGetValue(guildId, out var inner)
                    || !inner.TryGetValue(commandName, out var cdSeconds))
                {
                    _activeCooldowns.Remove((guildId, commandName), out _);
                    continue;
                }
                
                Cleanup(dict, cdSeconds);
            }
        }
    }

    private void Cleanup(ConcurrentDictionary<ulong, DateTime> dict, int cdSeconds)
    {
        var now = DateTime.UtcNow;
        foreach (var (key, _) in dict.Where(x => (now - x.Value).Seconds > cdSeconds).ToArray())
        {
            dict.TryRemove(key, out _);
        }
    }

    public void ClearCooldowns(ulong guildId, string cmdName)
    {
        if (_settings.TryGetValue(guildId, out var dict))
            dict.TryRemove(cmdName, out _);

        _activeCooldowns.TryRemove((guildId, cmdName), out _);
    }

    public void AddCooldown(ulong guildId, string name, int secs)
    {
        var sett = _settings.GetOrAdd(guildId, static _ => new());
        sett[name] = secs;

        // force cleanup 
        if (_activeCooldowns.TryGetValue((guildId, name), out var dict))
            Cleanup(dict, secs);
    }

    public IReadOnlyCollection<(string CommandName, int Seconds)> GetCommandCooldowns(ulong guildId)
    {
        if (!_settings.TryGetValue(guildId, out var dict))
            return Array.Empty<(string, int)>();

        return dict.Select(x => (x.Key, x.Value)).ToArray();
    }
}