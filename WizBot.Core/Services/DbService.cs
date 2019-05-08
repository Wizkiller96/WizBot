using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using WizBot.Core.Services.Database;
using System;
using System.IO;
using System.Linq;

namespace WizBot.Core.Services
{
    public class DbService
    {
        private readonly DbContextOptions<WizBotContext> options;
        private readonly DbContextOptions<WizBotContext> migrateOptions;

        private static readonly ILoggerFactory _loggerFactory = new LoggerFactory(new[] {
            new ConsoleLoggerProvider((category, level)
                => category == DbLoggerCategory.Database.Command.Name
                   && level >= LogLevel.Information, true)
            });

        public DbService(IBotCredentials creds)
        {
            var builder = new SqliteConnectionStringBuilder(creds.Db.ConnectionString);
            builder.DataSource = Path.Combine(AppContext.BaseDirectory, builder.DataSource);

            var optionsBuilder = new DbContextOptionsBuilder<WizBotContext>()
                //.UseLoggerFactory(_loggerFactory)
                ;
            optionsBuilder.UseSqlite(builder.ToString());
            options = optionsBuilder.Options;

            optionsBuilder = new DbContextOptionsBuilder<WizBotContext>();
            optionsBuilder.UseSqlite(builder.ToString(), x => x.SuppressForeignKeyEnforcement());
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
                context.Database.ExecuteSqlCommand("PRAGMA journal_mode=WAL");
                context.EnsureSeedData();
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

        public IUnitOfWork GetDbContext() => new UnitOfWork(GetDbContextInternal());
    }
}