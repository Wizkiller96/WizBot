using NadekoBot;
using NadekoBot.Services;
using OneOf;
using OneOf.Types;

namespace Nadeko.Bot.Common;

public interface IPermissionChecker
{
    Task<OneOf<Success, Error<LocStr>>> CheckAsync(IGuild guild,
        IMessageChannel channel,
        IUser author,
        string module,
        string cmd);
}