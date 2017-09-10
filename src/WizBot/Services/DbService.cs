using Microsoft.EntityFrameworkCore;
using WizBot.Services.Database;
using System.Linq;

namespace WizBot.Services
{
    public class DbService
    {
        private readonly DbContextOptions options;
        private readonly DbContextOptions migrateOptions;

        private readonly string _connectionString;

        public DbService(IBotCredentials creds)
        {
            _connectionString = creds.Db.ConnectionString;
            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseSqlite(creds.Db.ConnectionString);
            options = optionsBuilder.Options;

            optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseSqlite(creds.Db.ConnectionString, x => x.SuppressForeignKeyEnforcement());
            migrateOptions = optionsBuilder.Options;
        }

        public WizBotContext GetDbContext()
        {
            var context = new WizBotContext(options);
            if (context.Database.GetPendingMigrations().Any())
            {
                var mContext = new WizBotContext(migrateOptions);
                mContext.Database.Migrate();
                mContext.SaveChanges();
                mContext.Dispose();
            }
            context.Database.SetCommandTimeout(60);
            context.EnsureSeedData();

            //set important sqlite stuffs
            var conn = context.Database.GetDbConnection();
            conn.Open();

            context.Database.ExecuteSqlCommand("PRAGMA journal_mode=WAL");
            using (var com = conn.CreateCommand())
            {
                com.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=OFF";
                com.ExecuteNonQuery();
            }

            return context;
        }

        public IUnitOfWork UnitOfWork =>
            new UnitOfWork(GetDbContext());
    }
}