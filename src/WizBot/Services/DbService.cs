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

            return context;
        }

        public IUnitOfWork UnitOfWork =>
            new UnitOfWork(GetDbContext());
    }
}