using WizBot.Services.Database.Models;
using System.Collections.Generic;

namespace WizBot.Services.Database.Repositories
{
    public interface IPokeGameRepository : IRepository<UserPokeTypes>
    {
        //List<UserPokeTypes> GetAllPokeTypes();
    }
}
