#nullable disable
using Microsoft.EntityFrameworkCore;
using NadekoBot.Db.Models;

namespace NadekoBot.Db;

public static class ClubExtensions
{
    private static IQueryable<ClubInfo> Include(this DbSet<ClubInfo> clubs)
        => clubs.Include(x => x.Owner)
                .Include(x => x.Applicants)
                .ThenInclude(x => x.User)
                .Include(x => x.Bans)
                .ThenInclude(x => x.User)
                .Include(x => x.Users)
                .AsQueryable();

    public static ClubInfo GetByOwner(this DbSet<ClubInfo> clubs, ulong userId)
        => Include(clubs).FirstOrDefault(c => c.Owner.UserId == userId);

    public static ClubInfo GetByOwnerOrAdmin(this DbSet<ClubInfo> clubs, ulong userId)
        => Include(clubs)
            .FirstOrDefault(c => c.Owner.UserId == userId || c.Users.Any(u => u.UserId == userId && u.IsClubAdmin));

    public static ClubInfo GetByMember(this DbSet<ClubInfo> clubs, ulong userId)
        => Include(clubs).FirstOrDefault(c => c.Users.Any(u => u.UserId == userId));

    public static ClubInfo GetByName(this DbSet<ClubInfo> clubs, string name, int discrim)
        => Include(clubs)
            .FirstOrDefault(c => EF.Functions.Collate(c.Name, "NOCASE") == EF.Functions.Collate(name, "NOCASE")
                                 && c.Discrim == discrim);

    public static int GetNextDiscrim(this DbSet<ClubInfo> clubs, string name)
        => Include(clubs)
           .Where(x =>
               EF.Functions.Collate(x.Name, "NOCASE") == EF.Functions.Collate(name, "NOCASE"))
           .Select(x => x.Discrim)
           .DefaultIfEmpty()
           .Max()
           + 1;

    public static List<ClubInfo> GetClubLeaderboardPage(this DbSet<ClubInfo> clubs, int page)
        => clubs.AsNoTracking().OrderByDescending(x => x.Xp).Skip(page * 9).Take(9).ToList();
}