using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WizBot.Migrations;

public static class MigrationQueries
{
    public static void MigrateSar(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
                             INSERT INTO GroupName (Number, GuildConfigId)
                             SELECT DISTINCT "Group", GC.Id
                             FROM SelfAssignableRoles as SAR
                             INNER JOIN GuildConfigs as GC
                             ON SAR.GuildId = GC.GuildId
                             WHERE SAR.GuildId not in (SELECT GuildConfigs.GuildId from GroupName LEFT JOIN GuildConfigs ON GroupName.GuildConfigId = GuildConfigs.Id);

                             INSERT INTO SarGroup (Id, GroupNumber, Name, IsExclusive, GuildId)
                             SELECT GN.Id, GN.Number, GN.Name, GC.ExclusiveSelfAssignedRoles, GC.GuildId
                             FROM GroupName as GN
                             INNER JOIN GuildConfigs as GC ON GN.GuildConfigId = GC.Id;

                             INSERT INTO Sar (GuildId, RoleId, SarGroupId, LevelReq)
                             SELECT SAR.GuildId, SAR.RoleId, (SELECT Id FROM SarGroup WHERE SG.Number = SarGroup.GroupNumber AND SG.GuildId = SarGroup.GuildId), MIN(SAR.LevelRequirement)
                               FROM SelfAssignableRoles as SAR
                               INNER JOIN (SELECT GuildId, gn.Number FROM GroupName as gn
                             	INNER JOIN GuildConfigs as gc ON gn.GuildConfigId =gc.Id
                               ) as SG 
                             	ON SG.GuildId = SAR.GuildId 
                             WHERE SG.Number IN (SELECT GroupNumber FROM SarGroup WHERE Sar.GuildId = SarGroup.GuildId)
                             GROUP BY SAR.GuildId, SAR.RoleId;

                             INSERT INTO SarAutoDelete (GuildId, IsEnabled)
                             SELECT GuildId, AutoDeleteSelfAssignedRoleMessages FROM GuildConfigs WHERE AutoDeleteSelfAssignedRoleMessages = TRUE;
                             """);
    }

    public static void UpdateUsernames(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE DiscordUser SET Username = '??' || Username WHERE Discriminator = '????';");
    }

    public static void MigrateRero(MigrationBuilder migrationBuilder)
    {
        if (migrationBuilder.IsSqlite())
        {
            migrationBuilder.Sql(
                @"insert or ignore into reactionroles(guildid, channelid, messageid, emote, roleid, 'group', levelreq, dateadded)
select guildid, channelid, messageid, emotename, roleid, exclusive, 0, reactionrolemessage.dateadded
from reactionrole
left join reactionrolemessage on reactionrolemessage.id = reactionrole.reactionrolemessageid
left join guildconfigs on reactionrolemessage.guildconfigid = guildconfigs.id;");
        }
        else if (migrationBuilder.IsNpgsql())
        {
            migrationBuilder.Sql(
                @"insert into reactionroles(guildid, channelid, messageid, emote, roleid, ""group"", levelreq, dateadded)
            select guildid, channelid, messageid, emotename, roleid, exclusive::int, 0, reactionrolemessage.dateadded
                from reactionrole
                left join reactionrolemessage on reactionrolemessage.id = reactionrole.reactionrolemessageid
            left join guildconfigs on reactionrolemessage.guildconfigid = guildconfigs.id
            ON CONFLICT DO NOTHING;");
        }
        else
        {
            throw new NotSupportedException("This database provider doesn't have an implementation for MigrateRero");
        }
    }

    public static void GuildConfigCleanup(MigrationBuilder builder)
    {
        builder.Sql($"""
                     DELETE FROM "DelMsgOnCmdChannel" WHERE "GuildConfigId" is NULL;
                     DELETE FROM "WarningPunishment" WHERE "GuildConfigId" NOT IN (SELECT "Id" from "GuildConfigs");
                     DELETE FROM "StreamRoleBlacklistedUser" WHERE "StreamRoleSettingsId" is NULL;
                     DELETE FROM "Permissions" WHERE "GuildConfigId" NOT IN (SELECT "Id" from "GuildConfigs");
                     """);
    }

    public static void GreetSettingsCopy(MigrationBuilder builder)
    {
        builder.Sql("""
                    INSERT INTO GreetSettings (GuildId, GreetType, MessageText, IsEnabled, ChannelId, AutoDeleteTimer)
                    SELECT GuildId, 0, ChannelGreetMessageText, SendChannelGreetMessage, GreetMessageChannelId, AutoDeleteGreetMessagesTimer
                    FROM GuildConfigs
                    WHERE SendChannelGreetMessage = TRUE;

                    INSERT INTO GreetSettings (GuildId, GreetType, MessageText, IsEnabled, ChannelId, AutoDeleteTimer)
                    SELECT GuildId, 1, DmGreetMessageText, SendDmGreetMessage, GreetMessageChannelId, 0
                    FROM GuildConfigs
                    WHERE SendDmGreetMessage = TRUE;

                    INSERT INTO GreetSettings (GuildId, GreetType, MessageText, IsEnabled, ChannelId, AutoDeleteTimer)
                    SELECT GuildId, 2, ChannelByeMessageText, SendChannelByeMessage, ByeMessageChannelId, AutoDeleteByeMessagesTimer
                    FROM GuildConfigs
                    WHERE SendChannelByeMessage = TRUE;

                    INSERT INTO GreetSettings (GuildId, GreetType, MessageText, IsEnabled, ChannelId, AutoDeleteTimer)
                    SELECT GuildId, 3, BoostMessage, SendBoostMessage, BoostMessageChannelId, BoostMessageDeleteAfter
                    FROM GuildConfigs
                    WHERE SendBoostMessage = TRUE;
                    """);
    }
    
    public static void AddGuildIdsToWarningPunishment(MigrationBuilder builder)
    {
        builder.Sql("""
                    DELETE FROM WarningPunishment WHERE GuildConfigId IS NULL OR GuildConfigId NOT IN (SELECT Id FROM GuildConfigs);
                    UPDATE WarningPunishment 
                    SET GuildId = (SELECT GuildId FROM GuildConfigs WHERE Id = GuildConfigId);

                    DELETE FROM WarningPunishment as wp
                    WHERE (wp.Count, wp.GuildConfigId) in (
                        SELECT wp2.Count, wp2.GuildConfigId FROM WarningPunishment as wp2
                        GROUP BY wp2.Count, wp2.GuildConfigId
                        HAVING COUNT(id) > 1
                    );
                    """);
    }
}