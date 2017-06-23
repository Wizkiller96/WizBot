using Microsoft.EntityFrameworkCore;
using WizBot.Services.Database;

namespace WizBot.Services
{
    public class DbService
    {
        private readonly DbContextOptions options;

        private readonly string _connectionString;

        public DbService(IBotCredentials creds)
        {
            _connectionString = creds.Db.ConnectionString;
            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseSqlite(creds.Db.ConnectionString);
            options = optionsBuilder.Options;
            //switch (_creds.Db.Type.ToUpperInvariant())
            //{
            //    case "SQLITE":
            //        dbType = typeof(WizBotSqliteContext);
            //        break;
            //    //case "SQLSERVER":
            //    //    dbType = typeof(WizBotSqlServerContext);
            //    //    break;
            //    default:
            //        break;

            //}
        }

        public WizBotContext GetDbContext()
        {
            var context = new WizBotContext(options);
            context.Database.SetCommandTimeout(60);
            context.Database.Migrate();
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