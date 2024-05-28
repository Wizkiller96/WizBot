#nullable disable
namespace WizBot.Modules.Games.Common.ChatterBot;

public interface IChatterBotSession
{
    Task<string> Think(string input, string username);
}