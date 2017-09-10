using Microsoft.EntityFrameworkCore;
using WizBot.Services.Database.Models;
using System;
using System.Linq;

namespace WizBot.Services.Database.Repositories
{
    public interface IClubRepository : IRepository<ClubInfo>
    {
        int GetNextDiscrim(string clubName);
        ClubInfo GetByName(string v, int discrim, Func<DbSet<ClubInfo>, IQueryable<ClubInfo>> func = null);
        ClubInfo GetByOwner(ulong userId, Func<DbSet<ClubInfo>, IQueryable<ClubInfo>> func = null);
        ClubInfo GetByMember(ulong userId, Func<DbSet<ClubInfo>, IQueryable<ClubInfo>> func = null);
        ClubInfo[] GetClubLeaderboardPage(int page);
    }
}