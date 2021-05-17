using WizBot.Common;
using WizBot.Core.Services.Database.Models;
using System.Reflection;

namespace WizBot.Core.Services.Impl
{
    public class BotConfigProvider : IBotConfigProvider
    {
        private readonly DbService _db;
        private readonly IDataCache _cache;

        public BotConfig BotConfig { get; private set; }

        public BotConfigProvider(DbService db, IDataCache cache)
        {
            _db = db;
            _cache = cache;
            Reload();
        }

        public void Reload()
        {
            using (var uow = _db.GetDbContext())
            {
                BotConfig = uow.BotConfig.GetOrCreate();
            }
        }

        public bool Edit(BotConfigEditType type, string newValue)
        {
            using (var uow = _db.GetDbContext())
            {
                var bc = uow.BotConfig.GetOrCreate();
                switch (type)
                {
                    case BotConfigEditType.MinimumTriviaWinReq:
                        if (int.TryParse(newValue, out var req) && req >= 0)
                            bc.MinimumTriviaWinReq = req;
                        else
                            return false;
                        break;
                    default:
                        return false;
                }

                BotConfig = bc;
                uow.SaveChanges();
            }
            return true;
        }

        public string GetValue(string name)
        {
            var value = typeof(BotConfig)
                .GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(BotConfig);
            return value?.ToString() ?? "-";
        }
    }
}