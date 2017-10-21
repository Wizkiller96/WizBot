using WizBot.Core.Services.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace WizBot.Core.Services.Database.Repositories.Impl
{
    public class PokeGameRepository : Repository<UserPokeTypes>, IPokeGameRepository
    {
        public PokeGameRepository(DbContext context) : base(context)
        {

        }

        //List<UserPokeTypes> GetAllPokeTypes()
        //{
        //    var toReturn = _set.Include(pt => pt.UserId).ToList();
        //    toReturn.ForEach(pt => pt.).ToList();
        //    return toReturn;
        //}
    }
}
