using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WizBot.Migrations;

public static class MigrationQueries
{
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
}