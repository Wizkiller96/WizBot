#nullable disable
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Permissions.Services;

public class CmdCdService : IExecPreCommand, INService
{
    public ConcurrentDictionary<ulong, ConcurrentHashSet<CommandCooldown>> CommandCooldowns { get; }
    public ConcurrentDictionary<ulong, ConcurrentHashSet<ActiveCooldown>> ActiveCooldowns { get; } = new();

    public int Priority { get; } = 0;

    public CmdCdService(Bot bot)
        => CommandCooldowns = new(bot.AllGuildConfigs.ToDictionary(k => k.GuildId,
            v => new ConcurrentHashSet<CommandCooldown>(v.CommandCooldowns)));

    public Task<bool> TryBlock(IGuild guild, IUser user, string commandName)
    {
        if (guild is null)
            return Task.FromResult(false);

        var cmdcds = CommandCooldowns.GetOrAdd(guild.Id, new ConcurrentHashSet<CommandCooldown>());
        CommandCooldown cdRule;
        if ((cdRule = cmdcds.FirstOrDefault(cc => cc.CommandName == commandName)) is not null)
        {
            var activeCdsForGuild = ActiveCooldowns.GetOrAdd(guild.Id, new ConcurrentHashSet<ActiveCooldown>());
            if (activeCdsForGuild.FirstOrDefault(ac => ac.UserId == user.Id && ac.Command == commandName) is not null)
                return Task.FromResult(true);

            activeCdsForGuild.Add(new()
            {
                UserId = user.Id,
                Command = commandName
            });

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(cdRule.Seconds * 1000);
                    activeCdsForGuild.RemoveWhere(ac => ac.Command == commandName && ac.UserId == user.Id);
                }
                catch
                {
                    // ignored
                }
            });
        }

        return Task.FromResult(false);
    }

    public Task<bool> ExecPreCommandAsync(ICommandContext ctx, string moduleName, CommandInfo command)
    {
        var guild = ctx.Guild;
        var user = ctx.User;
        var commandName = command.Name.ToLowerInvariant();

        return TryBlock(guild, user, commandName);
    }
}

public class ActiveCooldown
{
    public string Command { get; set; }
    public ulong UserId { get; set; }
}