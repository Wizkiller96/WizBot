using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using WizBot.Core.Modules.Gambling.Common;
using WizBot.Core.Modules.Gambling.Services;
using NLog;

namespace WizBot.Core.Services
{
    public sealed class GamblingConfigMigrator : IConfigMigrator
    {
        private readonly Logger _log;
        private readonly DbService _db;
        private readonly GamblingConfigService _gss;

        public GamblingConfigMigrator(DbService dbService, GamblingConfigService gss)
        {
            _log = LogManager.GetCurrentClassLogger();
            _db = dbService;
            _gss = gss;
        }

        public void EnsureMigrated()
        {
            using var uow = _db.GetDbContext();
            using var conn = uow._context.Database.GetDbConnection();
            Migrate(conn);
        }

        private void Migrate(DbConnection conn)
        {
            using (var checkTableCommand = conn.CreateCommand())
            {
                // make sure table still exists
                checkTableCommand.CommandText =
                    "SELECT name FROM sqlite_master WHERE type='table' AND name='BotConfig';";
                var checkReader = checkTableCommand.ExecuteReader();
                if (!checkReader.HasRows)
                    return;
            }

            using (var checkMigratedCommand = conn.CreateCommand())
            {
                checkMigratedCommand.CommandText =
                    "UPDATE BotConfig SET HasMigratedGamblingSettings = 1 WHERE HasMigratedGamblingSettings = 0;";
                var changedRows = checkMigratedCommand.ExecuteNonQuery();
                if (changedRows == 0)
                    return;
            }

            _log.Info("Migrating gambling settings...");

            using var com = conn.CreateCommand();
            com.CommandText = $@"SELECT CurrencyGenerationChance, CurrencyGenerationCooldown,
CurrencySign, CurrencyName, CurrencyGenerationPassword, MinBet, MaxBet, BetflipMultiplier,
TimelyCurrency, TimelyCurrencyPeriod, CurrencyDropAmount, CurrencyDropAmountMax, DailyCurrencyDecay,
DivorcePriceMultiplier, PatreonCurrencyPerCent, MinWaifuPrice, WaifuGiftMultiplier
FROM BotConfig";

            using var reader = com.ExecuteReader();
            if (!reader.Read())
                return;


            using (var itemsCommand = conn.CreateCommand())
            {
                itemsCommand.CommandText = WaifuItemUpdateQuery;
                itemsCommand.ExecuteNonQuery();
            }


            _gss.ModifyConfig(ModifyAction(reader));

            _log.Info("Data written to data/gambling.yml");
        }

        private static Action<GamblingConfig> ModifyAction(DbDataReader reader)
            => realConfig =>
            {
                realConfig.Currency.Sign = (string) reader["CurrencySign"];
                realConfig.Currency.Name = (string) reader["CurrencyName"];
                realConfig.MinBet = (int) (long) reader["MinBet"];
                realConfig.MaxBet = (int) (long) reader["MaxBet"];
                realConfig.BetFlip = new GamblingConfig.BetFlipConfig()
                {
                    Multiplier = (decimal) (double) reader["BetflipMultiplier"],
                };
                realConfig.Generation = new GamblingConfig.GenerationConfig()
                {
                    MaxAmount = (int) (reader["CurrencyDropAmountMax"] as long? ?? (long) reader["CurrencyDropAmount"]),
                    MinAmount = (int) (long) reader["CurrencyDropAmount"],
                    Chance = (decimal) (double) reader["CurrencyGenerationChance"],
                    GenCooldown = (int) (long) reader["CurrencyGenerationCooldown"],
                    HasPassword = reader.GetBoolean(4),
                };
                realConfig.Timely = new GamblingConfig.TimelyConfig()
                {
                    Amount = (int) (long) reader["TimelyCurrency"],
                    Cooldown = (int) (long) reader["TimelyCurrencyPeriod"],
                };
                realConfig.Decay = new GamblingConfig.DecayConfig()
                    {Percent = (decimal) (double) reader["DailyCurrencyDecay"],};
                realConfig.Waifu = new GamblingConfig.WaifuConfig()
                {
                    MinPrice = (int) (long) reader["MinWaifuPrice"],
                    Multipliers = new GamblingConfig.WaifuConfig.MultipliersData()
                    {
                        AllGiftPrices = (decimal) (long) reader["WaifuGiftMultiplier"],
                        WaifuReset = (int) (long) reader["DivorcePriceMultiplier"]
                    }
                };
                realConfig.PatreonCurrencyPerCent = (decimal) (double) reader["PatreonCurrencyPerCent"];
            };

        private const string WaifuItemUpdateQuery = @"UPDATE WaifuItem
SET Name = CASE ItemEmoji
     WHEN '🥔' THEN 'potato'
     WHEN '🍪' THEN 'cookie'
     WHEN '🥖' THEN 'bread'
     WHEN '🍭' THEN 'lollipop'
     WHEN '🌹' THEN 'rose'
     WHEN '🍺' THEN 'beer'
     WHEN '🌮' THEN 'taco'
     WHEN '💌' THEN 'loveletter'
     WHEN '🥛' THEN 'milk'
     WHEN '🍕' THEN 'pizza'
     WHEN '🍫' THEN 'chocolate'
     WHEN '🍦' THEN 'icecream'
     WHEN '🍣' THEN 'sushi'
     WHEN '🍚' THEN 'rice'
     WHEN '🍉' THEN 'watermelon'
     WHEN '🍱' THEN 'bento'
     WHEN '🎟' THEN 'movieticket'
     WHEN '🍰' THEN 'cake'
     WHEN '📔' THEN 'book'
     WHEN '🐱' THEN 'cat'
     WHEN '🐶' THEN 'dog'
     WHEN '🐼' THEN 'panda'
     WHEN '💄' THEN 'lipstick'
     WHEN '👛' THEN 'purse'
     WHEN '📱' THEN 'iphone'
     WHEN '👗' THEN 'dress'
     WHEN '💻' THEN 'laptop'
     WHEN '🎻' THEN 'violin'
     WHEN '🎹' THEN 'piano'
     WHEN '🚗' THEN 'car'
     WHEN '💍' THEN 'ring'
     WHEN '🛳' THEN 'ship'
     WHEN '🏠' THEN 'house'
     WHEN '🚁' THEN 'helicopter'
     WHEN '🚀' THEN 'spaceship'
     WHEN '🌕' THEN 'moon'
     ELSE 'unknown'
    END
";
    }
}