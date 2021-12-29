#nullable disable
namespace NadekoBot.Modules.Games.Common.ChatterBot;

public interface IChatterBotSession
{
    Task<string> Think(string input);
}