#nullable disable
using LinqToDB.Common;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Services.Database;

namespace NadekoBot.Services;

public class DbService
{
    private readonly IBotCredsProvider _creds;

    // these are props because creds can change at runtime
    private string DbType => _creds.GetCreds().Db.Type.ToLowerInvariant().Trim();
    private string ConnString => _creds.GetCreds().Db.ConnectionString;
    
    public DbService(IBotCredsProvider creds)
    {
        LinqToDBForEFTools.Initialize();
        Configuration.Linq.DisableQueryCache = true;

        _creds = creds;
    }

    public async Task SetupAsync()
    {
        var dbType = DbType;
        var connString = ConnString;

        await using var context = CreateRawDbContext(dbType, connString);
        
        // make sure sqlite db is in wal journal mode
        if (context is SqliteContext)
        {
            await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL");
        }
        
        await context.Database.MigrateAsync();
    }

    private static NadekoContext CreateRawDbContext(string dbType, string connString)
    {
        switch (dbType)
        {
            case "postgresql":
            case "postgres":
            case "pgsql":
                return new PostgreSqlContext(connString);
            case "mysql":
                return new MysqlContext(connString);
            case "sqlite":
                return new SqliteContext(connString);
            default:
                throw new NotSupportedException($"The database provide type of '{dbType}' is not supported.");
        }
    }
    
    private NadekoContext GetDbContextInternal()
    {
        var dbType = DbType;
        var connString = ConnString;

        var context = CreateRawDbContext(dbType, connString);
        if (context is SqliteContext)
        {
            var conn = context.Database.GetDbConnection();
            conn.Open();
            using var com = conn.CreateCommand();
            com.CommandText = "PRAGMA synchronous=OFF";
            com.ExecuteNonQuery();
        }

        return context;
    }

    public NadekoContext GetDbContext()
        => GetDbContextInternal();
}