#nullable disable
using LinqToDB.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Services.Database;

namespace NadekoBot.Services;

public class DbService
{
    private readonly DbContextOptions<NadekoContext> _options;
    private readonly DbContextOptions<NadekoContext> _migrateOptions;

    public DbService(IBotCredentials creds)
    {
        LinqToDBForEFTools.Initialize();

        var builder = new SqliteConnectionStringBuilder(creds.Db.ConnectionString);
        builder.DataSource = Path.Combine(AppContext.BaseDirectory, builder.DataSource);

        var optionsBuilder = new DbContextOptionsBuilder<NadekoContext>();
        optionsBuilder.UseSqlite(builder.ToString());
        _options = optionsBuilder.Options;

        optionsBuilder = new();
        optionsBuilder.UseSqlite(builder.ToString());
        _migrateOptions = optionsBuilder.Options;
    }

    public void Setup()
    {
        using var context = new NadekoContext(_options);
        if (context.Database.GetPendingMigrations().Any())
        {
            using var mContext = new NadekoContext(_migrateOptions);
            mContext.Database.Migrate();
            mContext.SaveChanges();
        }

        context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL");
        context.SaveChanges();
    }

    private NadekoContext GetDbContextInternal()
    {
        var context = new NadekoContext(_options);
        context.Database.SetCommandTimeout(60);
        var conn = context.Database.GetDbConnection();
        conn.Open();
        using var com = conn.CreateCommand();
        com.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=OFF";
        com.ExecuteNonQuery();
        return context;
    }

    public NadekoContext GetDbContext()
        => GetDbContextInternal();
}