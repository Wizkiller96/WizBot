using WizBot.Services.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace WizBot.Services.Database.Repositories.Impl
{
    public class RepeaterRepository : Repository<Repeater>, IRepeaterRepository
    {
        public RepeaterRepository(DbContext context) : base(context)
        {
        }
    }
}
