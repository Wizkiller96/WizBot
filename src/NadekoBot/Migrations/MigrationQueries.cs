using Microsoft.EntityFrameworkCore.Migrations;

namespace NadekoBot.Migrations;

public static class MigrationQueries
{
    public static void MigrateRero(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            @"insert or ignore into reactionroles(guildid, channelid, messageid, emote, roleid, 'group', levelreq, dateadded)
select guildid, channelid, messageid, emotename, roleid, exclusive, 0, reactionrolemessage.dateadded
from reactionrole
left join reactionrolemessage on reactionrolemessage.id = reactionrole.reactionrolemessageid
left join guildconfigs on reactionrolemessage.guildconfigid = guildconfigs.id;");
    }
}