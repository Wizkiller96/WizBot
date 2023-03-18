using Nadeko.Bot.Common;
using NadekoBot.Modules.Permissions.Common;
using NadekoBot.Modules.Permissions.Services;
using OneOf;
using OneOf.Types;

namespace NadekoBot;

public sealed class PermissionChecker : IPermissionChecker, INService
{
    private readonly PermissionService _perms;
    private readonly GlobalPermissionService _gperm;
    private readonly CmdCdService _cmdCds;

    public PermissionChecker(PermissionService perms, GlobalPermissionService gperm, CmdCdService cmdCds)
    {
        _perms = perms;
        _gperm = gperm;
        _cmdCds = cmdCds;
    }

    public async Task<OneOf<Success, Error<LocStr>>> CheckAsync(
        IGuild guild,
        IMessageChannel channel,
        IUser author,
        string module,
        string cmd)
    {
        // todo add proper string
        if (await _cmdCds.TryBlock(guild, author, cmd))
            return new Error<LocStr>(new());

        try
        {
            if (_gperm.BlockedModules.Contains(module))
            {
                Log.Information("u:{UserId} tried to use module {Module} which is globally disabled.",
                    author.Id,
                    module
                );

                return new Success();
            }

            if (guild is SocketGuild sg)
            {
                var pc = _perms.GetCacheFor(guild.Id);
                if (!pc.Permissions.CheckPermissions(author, channel, cmd, "ACTUALEXPRESSIONS", out var index))
                {
                    if (pc.Verbose)
                    {
                        // todo fix
                        // var permissionMessage = strs.perm_prevent(index + 1,
                        //     Format.Bold(pc.Permissions[index].GetCommand(_cmd.GetPrefix(guild), sg)));
                        //
                        // try
                        // {
                        //     await msg.Channel.SendErrorAsync(_eb, permissionMessage);
                        // }
                        // catch
                        // {
                        // }
                        //
                        // Log.Information("{PermissionMessage}", permissionMessage);
                    }

                    // todo add proper string
                    return new Error<LocStr>(new());
                }
            }
        }
        catch
        {
        }

        return new Success();
    }
}