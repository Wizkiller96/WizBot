using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using WizBot.Core.Services;
using NLog;

namespace WizBot.Modules.Xp.Services
{
    public sealed class XpConfigMigrator : IConfigMigrator
    {
        private readonly Logger _log;
        private readonly DbService _db;
        private readonly XpConfigService _gss;

        public XpConfigMigrator(DbService dbService, XpConfigService gss)
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
                    "UPDATE BotConfig SET HasMigratedXpSettings = 1 WHERE HasMigratedXpSettings = 0;";
                var changedRows = checkMigratedCommand.ExecuteNonQuery();
                if (changedRows == 0)
                    return;
            }

            _log.Info("Migrating Xp settings...");

            using var com = conn.CreateCommand();
            com.CommandText = $@"SELECT XpPerMessage, XpMinutesTimeout, VoiceXpPerMinute, MaxXpMinutes 
FROM BotConfig";

            using var reader = com.ExecuteReader();
            if (!reader.Read())
                return;

            _gss.ModifyConfig(ModifyAction(reader));
        }

        private static Action<XpConfig> ModifyAction(DbDataReader reader)
            => config =>
            {
                config.XpPerMessage = (int) (long) reader["XpPerMessage"];
                config.MessageXpCooldown = (int) (long) reader["XpMinutesTimeout"];
                config.VoiceMaxMinutes = (int) (long) reader["MaxXpMinutes"];
                config.VoiceXpPerMinute = (double) reader["VoiceXpPerMinute"];
            };
    }
}