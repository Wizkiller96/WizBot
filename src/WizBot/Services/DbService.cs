#nullable disable
using LinqToDB.Common;
using LinqToDB.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WizBot.Services.Database;

namespace WizBot.Services;

public class DbService
{
    private readonly DbContextOptions<WizBotContext> _options;
    private readonly DbContextOptions<WizBotContext> _migrateOptions;

    public DbService(IBotCredentials creds)
    {
        LinqToDBForEFTools.Initialize();
        Configuration.Linq.DisableQueryCache = true;

        var builder = new SqliteConnectionStringBuilder(creds.Db.ConnectionString);
        builder.DataSource = Path.Combine(AppContext.BaseDirectory, builder.DataSource);

        var optionsBuilder = new DbContextOptionsBuilder<WizBotContext>();
        optionsBuilder.UseSqlite(builder.ToString());
        _options = optionsBuilder.Options;

        optionsBuilder = new();
        optionsBuilder.UseSqlite(builder.ToString());
        _migrateOptions = optionsBuilder.Options;
    }

    public void Setup()
    {
        using var context = new WizBotContext(_options);
        if (context.Database.GetPendingMigrations().Any())
        {
            using var mContext = new WizBotContext(_migrateOptions);
            mContext.Database.Migrate();
            mContext.SaveChanges();
        }

        context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL");
        context.SaveChanges();
    }

    private WizBotContext GetDbContextInternal()
    {
        var context = new WizBotContext(_options);
        context.Database.SetCommandTimeout(60);
        var conn = context.Database.GetDbConnection();
        conn.Open();
        using var com = conn.CreateCommand();
        com.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=OFF";
        com.ExecuteNonQuery();
        return context;
    }

    public WizBotContext GetDbContext()
        => GetDbContextInternal();
}