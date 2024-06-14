#nullable disable
using OneOf;
using OneOf.Types;

namespace WizBot.Modules.Games.Common.ChatterBot;

public interface IChatterBotSession
{
    Task<OneOf<ThinkResult, Error<string>>> Think(string input, string username);
}