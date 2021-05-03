using Discord;
using WizBot.Common;
using WizBot.Core.Services.Database.Models;
using System;
using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using WizBot.Core.Common;
using WizBot.Core.Common.Configs;
using NLog;
using NLog.Fluent;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WizBot.Core.Services.Impl
{
    public class BotSettingsMigrator
    {
        private readonly Logger _log;
        private readonly DbService _db;
        private readonly BotSettingsService _bss;

        public BotSettingsMigrator(DbService dbService, BotSettingsService bss)
        {
            _log = LogManager.GetCurrentClassLogger();
            _db = dbService;
            _bss = bss;
        }

        public void EnsureMigrated()
        {
            using (var uow = _db.GetDbContext())
            {
                var conn = uow._context.Database.GetDbConnection();
                MigrateBotConfig(conn);
            }
        }

        private void MigrateBotConfig(DbConnection conn)
        {
            using var checkTableCommand = conn.CreateCommand();

            // make sure table still exists
            checkTableCommand.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='BotConfig';";
            var checkReader = checkTableCommand.ExecuteReader();
            if (!checkReader.HasRows)
                return;

            using var checkMigratedCommand = conn.CreateCommand();
            checkMigratedCommand.CommandText = "UPDATE BotConfig SET HasMigratedBotSettings = 1 WHERE HasMigratedBotSettings = 0;";
            var changedRows = (int)checkMigratedCommand.ExecuteNonQuery();
            if (changedRows == 0)
                return;

            _log.Info("Migrating bot settings...");
            using var com = conn.CreateCommand();
            com.CommandText = $@"SELECT DefaultPrefix, ForwardMessages, ForwardToAllOwners,
OkColor, ErrorColor, ConsoleOutputType, DMHelpString, HelpString, PatreonCurrencyPerCent, RotatingStatuses
FROM BotConfig";

            using var reader = com.ExecuteReader();
            if (!reader.Read())
                return;

            Log.Info("Data written to data/bot.yml");

            _bss.ModifyBotConfig((BotSettings x) =>
            {
                x.Prefix = reader.GetString(0);
                x.ForwardMessages = reader.GetBoolean(1);
                x.ForwardToAllOwners = reader.GetBoolean(2);
                x.Color = new ColorConfig()
                {
                    Ok = reader.GetString(3),
                    Error = reader.GetString(4),
                };
                x.ConsoleOutputType = (ConsoleOutputType)reader.GetInt32(5);
                x.DmHelpText = reader.IsDBNull(7) ? string.Empty : reader.GetString(6);
                x.HelpText = reader.IsDBNull(8) ? string.Empty : reader.GetString(7);
                x.PatreonCurrencyPerCent = reader.GetFloat(8);
                x.RotateStatuses = reader.GetBoolean(9);
            });
        }
    }

    public abstract class SettingsServiceBase<TSettings>
    {
        protected readonly string _filePath;
        protected readonly ISeria _serializer;

        protected TSettings _data;
        public TSettings Data => CreateCopy();

        protected SettingsServiceBase(string filePath, ISeria serializer)
        {
            _filePath = filePath;
            _serializer = serializer;
        }

        private TSettings CreateCopy()
        {
            var serializedData = _serializer.Serialize(_data);
            return _serializer.Deserialize<TSettings>(serializedData);
        }

        public void Reload()
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            _data = deserializer.Deserialize<TSettings>(_filePath);
        }

        public void ModifyBotConfig(Action<TSettings> action)
        {
            var copy = CreateCopy();
            action(copy);
            _data = copy;
        }
    }

    public class BotSettingsService : SettingsServiceBase<BotSettings>
    {
        private const string FilePath = "config/bot.yml";
        public BotSettingsService(ISeria serializer) : base(FilePath, serializer)
        {

        }
    }

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
                    case BotConfigEditType.CurrencyGenerationChance:
                        if (float.TryParse(newValue, out var chance)
                            && chance >= 0
                            && chance <= 1)
                        {
                            bc.CurrencyGenerationChance = chance;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    case BotConfigEditType.CurrencyGenerationCooldown:
                        if (int.TryParse(newValue, out var cd) && cd >= 1)
                        {
                            bc.CurrencyGenerationCooldown = cd;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    case BotConfigEditType.CurrencyName:
                        bc.CurrencyName = newValue ?? "-";
                        break;
                    case BotConfigEditType.CurrencySign:
                        bc.CurrencySign = newValue ?? "-";
                        break;
                    case BotConfigEditType.DmHelpString:
                        bc.DMHelpString = string.IsNullOrWhiteSpace(newValue)
                            ? "-"
                            : newValue;
                        break;
                    case BotConfigEditType.HelpString:
                        bc.HelpString = string.IsNullOrWhiteSpace(newValue)
                            ? "-"
                            : newValue;
                        break;
                    case BotConfigEditType.CurrencyDropAmount:
                        if (int.TryParse(newValue, out var amount) && amount > 0)
                            bc.CurrencyDropAmount = amount;
                        else
                            return false;
                        break;
                    case BotConfigEditType.CurrencyDropAmountMax:
                        if (newValue == null)
                            bc.CurrencyDropAmountMax = null;
                        else if (int.TryParse(newValue, out var maxAmount) && maxAmount > 0)
                            bc.CurrencyDropAmountMax = maxAmount;
                        else
                            return false;
                        break;
                    case BotConfigEditType.TriviaCurrencyReward:
                        if (int.TryParse(newValue, out var triviaReward) && triviaReward >= 0)
                            bc.TriviaCurrencyReward = triviaReward;
                        else
                            return false;
                        break;
                    case BotConfigEditType.Betroll100Multiplier:
                        if (float.TryParse(newValue, out var br100) && br100 > 0)
                            bc.Betroll100Multiplier = br100;
                        else
                            return false;
                        break;
                    case BotConfigEditType.Betroll91Multiplier:
                        if (int.TryParse(newValue, out var br91) && br91 > 0)
                            bc.Betroll91Multiplier = br91;
                        else
                            return false;
                        break;
                    case BotConfigEditType.Betroll67Multiplier:
                        if (int.TryParse(newValue, out var br67) && br67 > 0)
                            bc.Betroll67Multiplier = br67;
                        else
                            return false;
                        break;
                    case BotConfigEditType.BetflipMultiplier:
                        if (float.TryParse(newValue, out var bf) && bf > 0)
                            bc.BetflipMultiplier = bf;
                        else
                            return false;
                        break;
                    case BotConfigEditType.DailyCurrencyDecay:
                        if (float.TryParse(newValue, out var decay) && decay >= 0)
                            bc.DailyCurrencyDecay = decay;
                        else
                            return false;
                        break;
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
                    case BotConfigEditType.PatreonCurrencyPerCent:
                        if (float.TryParse(newValue, out var cents) && cents > 0)
                            bc.PatreonCurrencyPerCent = cents;
                        else
                            return false;
                        break;
                    case BotConfigEditType.MinWaifuPrice:
                        if (int.TryParse(newValue, out var price) && price > 0)
                            bc.MinWaifuPrice = price;
                        else
                            return false;
                        break;
                    case BotConfigEditType.WaifuGiftMultiplier:
                        if (int.TryParse(newValue, out var mult) && mult > 0)
                            bc.WaifuGiftMultiplier = mult;
                        else
                            return false;
                        break;
                    case BotConfigEditType.MinimumTriviaWinReq:
                        if (int.TryParse(newValue, out var req) && req >= 0)
                            bc.MinimumTriviaWinReq = req;
                        else
                            return false;
                        break;
                    case BotConfigEditType.MinBet:
                        if (int.TryParse(newValue, out var gmin) && gmin >= 0)
                            bc.MinBet = gmin;
                        else
                            return false;
                        break;
                    case BotConfigEditType.MaxBet:
                        if (int.TryParse(newValue, out var gmax) && gmax >= 0)
                            bc.MaxBet = gmax;
                        else
                            return false;
                        break;
                    case BotConfigEditType.OkColor:
                        try
                        {
                            newValue = newValue.Replace("#", "", StringComparison.InvariantCulture);
                            var c = new Color(Convert.ToUInt32(newValue, 16));
                            WizBot.OkColor = c;
                            bc.OkColor = newValue;
                        }
                        catch
                        {
                            return false;
                        }
                        break;
                    case BotConfigEditType.ErrorColor:
                        try
                        {
                            newValue = newValue.Replace("#", "", StringComparison.InvariantCulture);
                            var c = new Color(Convert.ToUInt32(newValue, 16));
                            WizBot.ErrorColor = c;
                            bc.ErrorColor = newValue;
                        }
                        catch
                        {
                            return false;
                        }
                        break;
                    case BotConfigEditType.CurrencyGenerationPassword:
                        if (!bool.TryParse(newValue, out var pw))
                            return false;
                        bc.CurrencyGenerationPassword = pw;
                        break;
                    case BotConfigEditType.GroupGreets:
                        if (!bool.TryParse(newValue, out var groupGreets))
                            return false;
                        bc.GroupGreets = groupGreets;
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
