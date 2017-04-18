using WizBot.Services.Database.Models;
using System;

namespace WizBot.Services.Database.Repositories
{
    public interface IUnitConverterRepository : IRepository<ConvertUnit>
    {
        void AddOrUpdate(Func<ConvertUnit, bool> check, ConvertUnit toAdd, Func<ConvertUnit, ConvertUnit> toUpdate);
        bool Empty();
    }
}
