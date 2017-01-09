using System.Threading.Tasks;

namespace WizBot.Services
{
    public interface IStatsService
    {
        Task<string> Print();
        Task Reset();
    }
}
