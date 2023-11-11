#nullable disable
using LinqToDB.Common;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace NadekoBot.Services;

public abstract class DbService
{
    /// <summary>
    /// Call this to apply all migrations
    /// </summary>
    public abstract Task SetupAsync();

    public abstract DbContext CreateRawDbContext(string dbType, string connString);
    public abstract DbContext GetDbContext();
}