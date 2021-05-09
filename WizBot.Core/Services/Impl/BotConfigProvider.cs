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
                    case BotConfigEditType.XpPerMessage:
                        if (int.TryParse(newValue, out var xp) && xp > 0)
                            bc.XpPerMessage = xp;
                        else
                            return false;
                        break;
                    case BotConfigEditType.XpMinutesTimeout:
                        if (int.TryParse(newValue, out var min) && min > 0)
                            bc.XpMinutesTimeout = min;
                        else
                            return false;
                        break;
                    case BotConfigEditType.VoiceXpPerMinute:
                        if (double.TryParse(newValue, out var rate) && rate >= 0)
                            bc.VoiceXpPerMinute = rate;
                        else
                            return false;
                        break;
                    case BotConfigEditType.MaxXpMinutes:
                        if (int.TryParse(newValue, out var minutes) && minutes > 0)
                            bc.MaxXpMinutes = minutes;
                        else
                            return false;
                        break;
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