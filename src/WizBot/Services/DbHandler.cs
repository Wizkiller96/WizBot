using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using WizBot.Services.Database;

namespace WizBot.Services
{
    public class DbHandler
    {
        private static DbHandler _instance = null;
        public static DbHandler Instance = _instance ?? (_instance = new DbHandler());
        private readonly DbContextOptions options;

        private string connectionString { get; }

        static DbHandler() { }

        private DbHandler()
        {
            connectionString = WizBot.Credentials.Db.ConnectionString;
            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseSqlite(WizBot.Credentials.Db.ConnectionString);
            options = optionsBuilder.Options;
            //switch (WizBot.Credentials.Db.Type.ToUpperInvariant())
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

        private IUnitOfWork GetUnitOfWork() =>
            new UnitOfWork(GetDbContext());

        public static IUnitOfWork UnitOfWork() =>
            DbHandler.Instance.GetUnitOfWork();
    }
}