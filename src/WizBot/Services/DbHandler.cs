using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using WizBot.Services.Database;

namespace WizBot.Services
{
    public class DbHandler
    {
        private readonly DbContextOptions options;

        private string connectionString { get; }

        static DbHandler() { }

        public DbHandler(IBotCredentials creds)
        {
            connectionString = creds.Db.ConnectionString;
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
            context.Database.Migrate();
            context.EnsureSeedData();

            return context;
        }

        public IUnitOfWork UnitOfWork =>
            new UnitOfWork(GetDbContext());
    }
}