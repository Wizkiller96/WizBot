using WizBot.Core.Services.Database.Models;
using System.Collections.Generic;

namespace WizBot.Core.Services.Database.Repositories
{
    public interface IPollsRepository : IRepository<Poll>
    {
        IEnumerable<Poll> GetAllPolls();
        void RemovePoll(int id);
    }
}