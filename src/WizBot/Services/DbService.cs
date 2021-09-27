using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WizBot.Services.Database;
using System;
using System.IO;
using System.Linq;
using LinqToDB.EntityFrameworkCore;

namespace WizBot.Services
{
    public class DbService
    {
        private readonly DbContextOptions<WizBotContext> options;
        private readonly DbContextOptions<WizBotContext> migrateOptions;

        public DbService(IBotCredentials creds)
        {
            LinqToDBForEFTools.Initialize();
            
            var builder = new SqliteConnectionStringBuilder(creds.Db.ConnectionString);
            builder.DataSource = Path.Combine(AppContext.BaseDirectory, builder.DataSource);

            var optionsBuilder = new DbContextOptionsBuilder<WizBotContext>();
            optionsBuilder.UseSqlite(builder.ToString());
            options = optionsBuilder.Options;

            optionsBuilder = new DbContextOptionsBuilder<WizBotContext>();
            optionsBuilder.UseSqlite(builder.ToString());
            migrateOptions = optionsBuilder.Options;
        }

        public void Setup()
        {
            using (var context = new WizBotContext(options))
            {
                if (context.Database.GetPendingMigrations().Any())
                {
                    var mContext = new WizBotContext(migrateOptions);
                    mContext.Database.Migrate();
                    mContext.SaveChanges();
                    mContext.Dispose();
                }
                context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL");
                context.SaveChanges();
            }
        }

        private WizBotContext GetDbContextInternal()
        {
            var context = new WizBotContext(options);
            context.Database.SetCommandTimeout(60);
            var conn = context.Database.GetDbConnection();
            conn.Open();
            using (var com = conn.CreateCommand())
            {
                com.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=OFF";
                com.ExecuteNonQuery();
            }
            return context;
        }

        public WizBotContext GetDbContext() => GetDbContextInternal();
    }
}