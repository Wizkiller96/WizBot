using WizBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace WizBot.Services.Database.Repositories.Impl
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
