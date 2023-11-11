#nullable disable
using Microsoft.EntityFrameworkCore;
using Nadeko.Bot.Db.Models;

namespace NadekoBot.Db;

public static class DbExtensions
{
    public static T GetById<T>(this DbSet<T> set, int id)
        where T : DbEntity
        => set.FirstOrDefault(x => x.Id == id);
}