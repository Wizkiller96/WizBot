using System.Threading.Tasks;

namespace WizBot.Modules.Games.Common.ChatterBot
{
    public interface IChatterBotSession
    {
        Task<string> Think(string input);
    }
}
