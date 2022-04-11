using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NadekoBot.Migrations.PostgreSql
{
    public partial class mysqlinit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "autocommands",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    commandtext = table.Column<string>(type: "text", nullable: true),
                    channelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    channelname = table.Column<string>(type: "text", nullable: true),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    guildname = table.Column<string>(type: "text", nullable: true),
                    voicechannelid = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    voicechannelname = table.Column<string>(type: "text", nullable: true),
                    interval = table.Column<int>(type: "integer", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_autocommands", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "autotranslatechannels",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    channelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    autodelete = table.Column<bool>(type: "boolean", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_autotranslatechannels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bantemplates",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    text = table.Column<string>(type: "text", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bantemplates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "blacklist",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    itemid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_blacklist", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "currencytransactions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    extra = table.Column<string>(type: "text", nullable: false),
                    otherid = table.Column<decimal>(type: "numeric(20,0)", nullable: true, defaultValueSql: "NULL"),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_currencytransactions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "discordpermoverrides",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    perm = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    command = table.Column<string>(type: "text", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discordpermoverrides", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "expressions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    response = table.Column<string>(type: "text", nullable: true),
                    trigger = table.Column<string>(type: "text", nullable: true),
                    autodeletetrigger = table.Column<bool>(type: "boolean", nullable: false),
                    dmresponse = table.Column<bool>(type: "boolean", nullable: false),
                    containsanywhere = table.Column<bool>(type: "boolean", nullable: false),
                    allowtarget = table.Column<bool>(type: "boolean", nullable: false),
                    reactions = table.Column<string>(type: "text", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expressions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "guildconfigs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    prefix = table.Column<string>(type: "text", nullable: true),
                    deletemessageoncommand = table.Column<bool>(type: "boolean", nullable: false),
                    autoassignroleids = table.Column<string>(type: "text", nullable: true),
                    autodeletegreetmessagestimer = table.Column<int>(type: "integer", nullable: false),
                    autodeletebyemessagestimer = table.Column<int>(type: "integer", nullable: false),
                    greetmessagechannelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    byemessagechannelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    senddmgreetmessage = table.Column<bool>(type: "boolean", nullable: false),
                    dmgreetmessagetext = table.Column<string>(type: "text", nullable: true),
                    sendchannelgreetmessage = table.Column<bool>(type: "boolean", nullable: false),
                    channelgreetmessagetext = table.Column<string>(type: "text", nullable: true),
                    sendchannelbyemessage = table.Column<bool>(type: "boolean", nullable: false),
                    channelbyemessagetext = table.Column<string>(type: "text", nullable: true),
                    exclusiveselfassignedroles = table.Column<bool>(type: "boolean", nullable: false),
                    autodeleteselfassignedrolemessages = table.Column<bool>(type: "boolean", nullable: false),
                    verbosepermissions = table.Column<bool>(type: "boolean", nullable: false),
                    permissionrole = table.Column<string>(type: "text", nullable: true),
                    filterinvites = table.Column<bool>(type: "boolean", nullable: false),
                    filterlinks = table.Column<bool>(type: "boolean", nullable: false),
                    filterwords = table.Column<bool>(type: "boolean", nullable: false),
                    muterolename = table.Column<string>(type: "text", nullable: true),
                    cleverbotenabled = table.Column<bool>(type: "boolean", nullable: false),
                    locale = table.Column<string>(type: "text", nullable: true),
                    timezoneid = table.Column<string>(type: "text", nullable: true),
                    warningsinitialized = table.Column<bool>(type: "boolean", nullable: false),
                    gamevoicechannel = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    verboseerrors = table.Column<bool>(type: "boolean", nullable: false),
                    notifystreamoffline = table.Column<bool>(type: "boolean", nullable: false),
                    warnexpirehours = table.Column<int>(type: "integer", nullable: false),
                    warnexpireaction = table.Column<int>(type: "integer", nullable: false),
                    sendboostmessage = table.Column<bool>(type: "boolean", nullable: false),
                    boostmessage = table.Column<string>(type: "text", nullable: true),
                    boostmessagechannelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    boostmessagedeleteafter = table.Column<int>(type: "integer", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guildconfigs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "imageonlychannels",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    channelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_imageonlychannels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "logsettings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    logotherid = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    messageupdatedid = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    messagedeletedid = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    userjoinedid = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    userleftid = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    userbannedid = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    userunbannedid = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    userupdatedid = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    channelcreatedid = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    channeldestroyedid = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    channelupdatedid = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    usermutedid = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    loguserpresenceid = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    logvoicepresenceid = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    logvoicepresencettsid = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_logsettings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "musicplayersettings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    playerrepeat = table.Column<int>(type: "integer", nullable: false),
                    musicchannelid = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    volume = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    autodisconnect = table.Column<bool>(type: "boolean", nullable: false),
                    qualitypreset = table.Column<int>(type: "integer", nullable: false),
                    autoplay = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_musicplayersettings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "musicplaylists",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: true),
                    author = table.Column<string>(type: "text", nullable: true),
                    authorid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_musicplaylists", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "nsfwblacklistedtags",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    tag = table.Column<string>(type: "text", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nsfwblacklistedtags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "plantedcurrency",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    password = table.Column<string>(type: "text", nullable: true),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    channelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    messageid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plantedcurrency", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "poll",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    channelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    question = table.Column<string>(type: "text", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_poll", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "quotes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    keyword = table.Column<string>(type: "text", nullable: false),
                    authorname = table.Column<string>(type: "text", nullable: false),
                    authorid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quotes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reminders",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    when = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    channelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    serverid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    message = table.Column<string>(type: "text", nullable: true),
                    isprivate = table.Column<bool>(type: "boolean", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reminders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "repeaters",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    channelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    lastmessageid = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    message = table.Column<string>(type: "text", nullable: true),
                    interval = table.Column<TimeSpan>(type: "interval", nullable: false),
                    starttimeofday = table.Column<TimeSpan>(type: "interval", nullable: true),
                    noredundant = table.Column<bool>(type: "boolean", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_repeaters", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rewardedusers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    patreonuserid = table.Column<string>(type: "text", nullable: true),
                    amountrewardedthismonth = table.Column<int>(type: "integer", nullable: false),
                    lastreward = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rewardedusers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rotatingstatus",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    status = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rotatingstatus", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "selfassignableroles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    roleid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    group = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    levelrequirement = table.Column<int>(type: "integer", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_selfassignableroles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "userxpstats",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    xp = table.Column<int>(type: "integer", nullable: false),
                    awardedxp = table.Column<int>(type: "integer", nullable: false),
                    notifyonlevelup = table.Column<int>(type: "integer", nullable: false),
                    lastlevelup = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_userxpstats", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "warnings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    forgiven = table.Column<bool>(type: "boolean", nullable: false),
                    forgivenby = table.Column<string>(type: "text", nullable: true),
                    moderator = table.Column<string>(type: "text", nullable: true),
                    weight = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warnings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "autotranslateusers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    channelid = table.Column<int>(type: "integer", nullable: false),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    source = table.Column<string>(type: "text", nullable: true),
                    target = table.Column<string>(type: "text", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_autotranslateusers", x => x.id);
                    table.UniqueConstraint("ak_autotranslateusers_channelid_userid", x => new { x.channelid, x.userid });
                    table.ForeignKey(
                        name: "fk_autotranslateusers_autotranslatechannels_channelid",
                        column: x => x.channelid,
                        principalTable: "autotranslatechannels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "antialtsetting",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildconfigid = table.Column<int>(type: "integer", nullable: false),
                    minage = table.Column<TimeSpan>(type: "interval", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    actiondurationminutes = table.Column<int>(type: "integer", nullable: false),
                    roleid = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_antialtsetting", x => x.id);
                    table.ForeignKey(
                        name: "fk_antialtsetting_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "antiraidsetting",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildconfigid = table.Column<int>(type: "integer", nullable: false),
                    userthreshold = table.Column<int>(type: "integer", nullable: false),
                    seconds = table.Column<int>(type: "integer", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    punishduration = table.Column<int>(type: "integer", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_antiraidsetting", x => x.id);
                    table.ForeignKey(
                        name: "fk_antiraidsetting_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "antispamsetting",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildconfigid = table.Column<int>(type: "integer", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    messagethreshold = table.Column<int>(type: "integer", nullable: false),
                    mutetime = table.Column<int>(type: "integer", nullable: false),
                    roleid = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_antispamsetting", x => x.id);
                    table.ForeignKey(
                        name: "fk_antispamsetting_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "commandalias",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    trigger = table.Column<string>(type: "text", nullable: true),
                    mapping = table.Column<string>(type: "text", nullable: true),
                    guildconfigid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_commandalias", x => x.id);
                    table.ForeignKey(
                        name: "fk_commandalias_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "commandcooldown",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    seconds = table.Column<int>(type: "integer", nullable: false),
                    commandname = table.Column<string>(type: "text", nullable: true),
                    guildconfigid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_commandcooldown", x => x.id);
                    table.ForeignKey(
                        name: "fk_commandcooldown_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "delmsgoncmdchannel",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    channelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    guildconfigid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delmsgoncmdchannel", x => x.id);
                    table.ForeignKey(
                        name: "fk_delmsgoncmdchannel_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "feedsub",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildconfigid = table.Column<int>(type: "integer", nullable: false),
                    channelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feedsub", x => x.id);
                    table.UniqueConstraint("ak_feedsub_guildconfigid_url", x => new { x.guildconfigid, x.url });
                    table.ForeignKey(
                        name: "fk_feedsub_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "filterchannelid",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    channelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    guildconfigid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filterchannelid", x => x.id);
                    table.ForeignKey(
                        name: "fk_filterchannelid_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "filteredword",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    word = table.Column<string>(type: "text", nullable: true),
                    guildconfigid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filteredword", x => x.id);
                    table.ForeignKey(
                        name: "fk_filteredword_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "filterlinkschannelid",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    channelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    guildconfigid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filterlinkschannelid", x => x.id);
                    table.ForeignKey(
                        name: "fk_filterlinkschannelid_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "filterwordschannelid",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    channelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    guildconfigid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filterwordschannelid", x => x.id);
                    table.ForeignKey(
                        name: "fk_filterwordschannelid_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "followedstream",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    channelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    username = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    message = table.Column<string>(type: "text", nullable: true),
                    guildconfigid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_followedstream", x => x.id);
                    table.ForeignKey(
                        name: "fk_followedstream_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "gcchannelid",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildconfigid = table.Column<int>(type: "integer", nullable: true),
                    channelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gcchannelid", x => x.id);
                    table.ForeignKey(
                        name: "fk_gcchannelid_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "groupname",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildconfigid = table.Column<int>(type: "integer", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_groupname", x => x.id);
                    table.ForeignKey(
                        name: "fk_groupname_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "muteduserid",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    guildconfigid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_muteduserid", x => x.id);
                    table.ForeignKey(
                        name: "fk_muteduserid_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildconfigid = table.Column<int>(type: "integer", nullable: true),
                    index = table.Column<int>(type: "integer", nullable: false),
                    primarytarget = table.Column<int>(type: "integer", nullable: false),
                    primarytargetid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    secondarytarget = table.Column<int>(type: "integer", nullable: false),
                    secondarytargetname = table.Column<string>(type: "text", nullable: true),
                    iscustomcommand = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_permissions_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "reactionrolemessage",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    index = table.Column<int>(type: "integer", nullable: false),
                    guildconfigid = table.Column<int>(type: "integer", nullable: false),
                    channelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    messageid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    exclusive = table.Column<bool>(type: "boolean", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reactionrolemessage", x => x.id);
                    table.ForeignKey(
                        name: "fk_reactionrolemessage_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shopentry",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    index = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: true),
                    authorid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    rolename = table.Column<string>(type: "text", nullable: true),
                    roleid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    guildconfigid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shopentry", x => x.id);
                    table.ForeignKey(
                        name: "fk_shopentry_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "slowmodeignoredrole",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    roleid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    guildconfigid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_slowmodeignoredrole", x => x.id);
                    table.ForeignKey(
                        name: "fk_slowmodeignoredrole_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "slowmodeignoreduser",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    guildconfigid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_slowmodeignoreduser", x => x.id);
                    table.ForeignKey(
                        name: "fk_slowmodeignoreduser_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "streamrolesettings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildconfigid = table.Column<int>(type: "integer", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    addroleid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    fromroleid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    keyword = table.Column<string>(type: "text", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_streamrolesettings", x => x.id);
                    table.ForeignKey(
                        name: "fk_streamrolesettings_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "unbantimer",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    unbanat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    guildconfigid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unbantimer", x => x.id);
                    table.ForeignKey(
                        name: "fk_unbantimer_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "unmutetimer",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    unmuteat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    guildconfigid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unmutetimer", x => x.id);
                    table.ForeignKey(
                        name: "fk_unmutetimer_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "unroletimer",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    roleid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    unbanat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    guildconfigid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unroletimer", x => x.id);
                    table.ForeignKey(
                        name: "fk_unroletimer_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "vcroleinfo",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    voicechannelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    roleid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    guildconfigid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vcroleinfo", x => x.id);
                    table.ForeignKey(
                        name: "fk_vcroleinfo_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "warningpunishment",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    count = table.Column<int>(type: "integer", nullable: false),
                    punishment = table.Column<int>(type: "integer", nullable: false),
                    time = table.Column<int>(type: "integer", nullable: false),
                    roleid = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    guildconfigid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warningpunishment", x => x.id);
                    table.ForeignKey(
                        name: "fk_warningpunishment_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "xpsettings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildconfigid = table.Column<int>(type: "integer", nullable: false),
                    serverexcluded = table.Column<bool>(type: "boolean", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_xpsettings", x => x.id);
                    table.ForeignKey(
                        name: "fk_xpsettings_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ignoredlogchannels",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    logsettingid = table.Column<int>(type: "integer", nullable: false),
                    logitemid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    itemtype = table.Column<int>(type: "integer", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ignoredlogchannels", x => x.id);
                    table.ForeignKey(
                        name: "fk_ignoredlogchannels_logsettings_logsettingid",
                        column: x => x.logsettingid,
                        principalTable: "logsettings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ignoredvoicepresencechannels",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    logsettingid = table.Column<int>(type: "integer", nullable: true),
                    channelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ignoredvoicepresencechannels", x => x.id);
                    table.ForeignKey(
                        name: "fk_ignoredvoicepresencechannels_logsettings_logsettingid",
                        column: x => x.logsettingid,
                        principalTable: "logsettings",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "playlistsong",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    provider = table.Column<string>(type: "text", nullable: true),
                    providertype = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "text", nullable: true),
                    uri = table.Column<string>(type: "text", nullable: true),
                    query = table.Column<string>(type: "text", nullable: true),
                    musicplaylistid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_playlistsong", x => x.id);
                    table.ForeignKey(
                        name: "fk_playlistsong_musicplaylists_musicplaylistid",
                        column: x => x.musicplaylistid,
                        principalTable: "musicplaylists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pollanswer",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    index = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "text", nullable: true),
                    pollid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pollanswer", x => x.id);
                    table.ForeignKey(
                        name: "fk_pollanswer_poll_pollid",
                        column: x => x.pollid,
                        principalTable: "poll",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "pollvote",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    voteindex = table.Column<int>(type: "integer", nullable: false),
                    pollid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pollvote", x => x.id);
                    table.ForeignKey(
                        name: "fk_pollvote_poll_pollid",
                        column: x => x.pollid,
                        principalTable: "poll",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "antispamignore",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    channelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    antispamsettingid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_antispamignore", x => x.id);
                    table.ForeignKey(
                        name: "fk_antispamignore_antispamsetting_antispamsettingid",
                        column: x => x.antispamsettingid,
                        principalTable: "antispamsetting",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "reactionrole",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    emotename = table.Column<string>(type: "text", nullable: true),
                    roleid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    reactionrolemessageid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reactionrole", x => x.id);
                    table.ForeignKey(
                        name: "fk_reactionrole_reactionrolemessage_reactionrolemessageid",
                        column: x => x.reactionrolemessageid,
                        principalTable: "reactionrolemessage",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shopentryitem",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    text = table.Column<string>(type: "text", nullable: true),
                    shopentryid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shopentryitem", x => x.id);
                    table.ForeignKey(
                        name: "fk_shopentryitem_shopentry_shopentryid",
                        column: x => x.shopentryid,
                        principalTable: "shopentry",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "streamroleblacklisteduser",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    username = table.Column<string>(type: "text", nullable: true),
                    streamrolesettingsid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_streamroleblacklisteduser", x => x.id);
                    table.ForeignKey(
                        name: "fk_streamroleblacklisteduser_streamrolesettings_streamrolesett~",
                        column: x => x.streamrolesettingsid,
                        principalTable: "streamrolesettings",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "streamrolewhitelisteduser",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    username = table.Column<string>(type: "text", nullable: true),
                    streamrolesettingsid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_streamrolewhitelisteduser", x => x.id);
                    table.ForeignKey(
                        name: "fk_streamrolewhitelisteduser_streamrolesettings_streamrolesett~",
                        column: x => x.streamrolesettingsid,
                        principalTable: "streamrolesettings",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "excludeditem",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    itemid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    itemtype = table.Column<int>(type: "integer", nullable: false),
                    xpsettingsid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_excludeditem", x => x.id);
                    table.ForeignKey(
                        name: "fk_excludeditem_xpsettings_xpsettingsid",
                        column: x => x.xpsettingsid,
                        principalTable: "xpsettings",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "xpcurrencyreward",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    xpsettingsid = table.Column<int>(type: "integer", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<int>(type: "integer", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_xpcurrencyreward", x => x.id);
                    table.ForeignKey(
                        name: "fk_xpcurrencyreward_xpsettings_xpsettingsid",
                        column: x => x.xpsettingsid,
                        principalTable: "xpsettings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "xprolereward",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    xpsettingsid = table.Column<int>(type: "integer", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    roleid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    remove = table.Column<bool>(type: "boolean", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_xprolereward", x => x.id);
                    table.ForeignKey(
                        name: "fk_xprolereward_xpsettings_xpsettingsid",
                        column: x => x.xpsettingsid,
                        principalTable: "xpsettings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "clubapplicants",
                columns: table => new
                {
                    clubid = table.Column<int>(type: "integer", nullable: false),
                    userid = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clubapplicants", x => new { x.clubid, x.userid });
                });

            migrationBuilder.CreateTable(
                name: "clubbans",
                columns: table => new
                {
                    clubid = table.Column<int>(type: "integer", nullable: false),
                    userid = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clubbans", x => new { x.clubid, x.userid });
                });

            migrationBuilder.CreateTable(
                name: "clubs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    imageurl = table.Column<string>(type: "text", nullable: true),
                    xp = table.Column<int>(type: "integer", nullable: false),
                    ownerid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clubs", x => x.id);
                    table.UniqueConstraint("ak_clubs_name", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "discorduser",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    username = table.Column<string>(type: "text", nullable: true),
                    discriminator = table.Column<string>(type: "text", nullable: true),
                    avatarid = table.Column<string>(type: "text", nullable: true),
                    clubid = table.Column<int>(type: "integer", nullable: true),
                    isclubadmin = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    totalxp = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    lastlevelup = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    lastxpgain = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now()) - interval '-1 year'"),
                    notifyonlevelup = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    currencyamount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discorduser", x => x.id);
                    table.UniqueConstraint("ak_discorduser_userid", x => x.userid);
                    table.ForeignKey(
                        name: "fk_discorduser_clubs_clubid",
                        column: x => x.clubid,
                        principalTable: "clubs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "waifuinfo",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    waifuid = table.Column<int>(type: "integer", nullable: false),
                    claimerid = table.Column<int>(type: "integer", nullable: true),
                    affinityid = table.Column<int>(type: "integer", nullable: true),
                    price = table.Column<long>(type: "bigint", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_waifuinfo", x => x.id);
                    table.ForeignKey(
                        name: "fk_waifuinfo_discorduser_affinityid",
                        column: x => x.affinityid,
                        principalTable: "discorduser",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_waifuinfo_discorduser_claimerid",
                        column: x => x.claimerid,
                        principalTable: "discorduser",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_waifuinfo_discorduser_waifuid",
                        column: x => x.waifuid,
                        principalTable: "discorduser",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "waifuupdates",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    updatetype = table.Column<int>(type: "integer", nullable: false),
                    oldid = table.Column<int>(type: "integer", nullable: true),
                    newid = table.Column<int>(type: "integer", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_waifuupdates", x => x.id);
                    table.ForeignKey(
                        name: "fk_waifuupdates_discorduser_newid",
                        column: x => x.newid,
                        principalTable: "discorduser",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_waifuupdates_discorduser_oldid",
                        column: x => x.oldid,
                        principalTable: "discorduser",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_waifuupdates_discorduser_userid",
                        column: x => x.userid,
                        principalTable: "discorduser",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "waifuitem",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    waifuinfoid = table.Column<int>(type: "integer", nullable: true),
                    itememoji = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: true),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_waifuitem", x => x.id);
                    table.ForeignKey(
                        name: "fk_waifuitem_waifuinfo_waifuinfoid",
                        column: x => x.waifuinfoid,
                        principalTable: "waifuinfo",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_antialtsetting_guildconfigid",
                table: "antialtsetting",
                column: "guildconfigid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_antiraidsetting_guildconfigid",
                table: "antiraidsetting",
                column: "guildconfigid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_antispamignore_antispamsettingid",
                table: "antispamignore",
                column: "antispamsettingid");

            migrationBuilder.CreateIndex(
                name: "ix_antispamsetting_guildconfigid",
                table: "antispamsetting",
                column: "guildconfigid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_autotranslatechannels_channelid",
                table: "autotranslatechannels",
                column: "channelid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_autotranslatechannels_guildid",
                table: "autotranslatechannels",
                column: "guildid");

            migrationBuilder.CreateIndex(
                name: "ix_bantemplates_guildid",
                table: "bantemplates",
                column: "guildid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_clubapplicants_userid",
                table: "clubapplicants",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "ix_clubbans_userid",
                table: "clubbans",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "ix_clubs_ownerid",
                table: "clubs",
                column: "ownerid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_commandalias_guildconfigid",
                table: "commandalias",
                column: "guildconfigid");

            migrationBuilder.CreateIndex(
                name: "ix_commandcooldown_guildconfigid",
                table: "commandcooldown",
                column: "guildconfigid");

            migrationBuilder.CreateIndex(
                name: "ix_currencytransactions_userid",
                table: "currencytransactions",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "ix_delmsgoncmdchannel_guildconfigid",
                table: "delmsgoncmdchannel",
                column: "guildconfigid");

            migrationBuilder.CreateIndex(
                name: "ix_discordpermoverrides_guildid_command",
                table: "discordpermoverrides",
                columns: new[] { "guildid", "command" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_discorduser_clubid",
                table: "discorduser",
                column: "clubid");

            migrationBuilder.CreateIndex(
                name: "ix_discorduser_currencyamount",
                table: "discorduser",
                column: "currencyamount");

            migrationBuilder.CreateIndex(
                name: "ix_discorduser_totalxp",
                table: "discorduser",
                column: "totalxp");

            migrationBuilder.CreateIndex(
                name: "ix_discorduser_userid",
                table: "discorduser",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "ix_excludeditem_xpsettingsid",
                table: "excludeditem",
                column: "xpsettingsid");

            migrationBuilder.CreateIndex(
                name: "ix_filterchannelid_guildconfigid",
                table: "filterchannelid",
                column: "guildconfigid");

            migrationBuilder.CreateIndex(
                name: "ix_filteredword_guildconfigid",
                table: "filteredword",
                column: "guildconfigid");

            migrationBuilder.CreateIndex(
                name: "ix_filterlinkschannelid_guildconfigid",
                table: "filterlinkschannelid",
                column: "guildconfigid");

            migrationBuilder.CreateIndex(
                name: "ix_filterwordschannelid_guildconfigid",
                table: "filterwordschannelid",
                column: "guildconfigid");

            migrationBuilder.CreateIndex(
                name: "ix_followedstream_guildconfigid",
                table: "followedstream",
                column: "guildconfigid");

            migrationBuilder.CreateIndex(
                name: "ix_gcchannelid_guildconfigid",
                table: "gcchannelid",
                column: "guildconfigid");

            migrationBuilder.CreateIndex(
                name: "ix_groupname_guildconfigid_number",
                table: "groupname",
                columns: new[] { "guildconfigid", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_guildconfigs_guildid",
                table: "guildconfigs",
                column: "guildid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_guildconfigs_warnexpirehours",
                table: "guildconfigs",
                column: "warnexpirehours");

            migrationBuilder.CreateIndex(
                name: "ix_ignoredlogchannels_logsettingid_logitemid_itemtype",
                table: "ignoredlogchannels",
                columns: new[] { "logsettingid", "logitemid", "itemtype" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ignoredvoicepresencechannels_logsettingid",
                table: "ignoredvoicepresencechannels",
                column: "logsettingid");

            migrationBuilder.CreateIndex(
                name: "ix_imageonlychannels_channelid",
                table: "imageonlychannels",
                column: "channelid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_logsettings_guildid",
                table: "logsettings",
                column: "guildid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_musicplayersettings_guildid",
                table: "musicplayersettings",
                column: "guildid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_muteduserid_guildconfigid",
                table: "muteduserid",
                column: "guildconfigid");

            migrationBuilder.CreateIndex(
                name: "ix_nsfwblacklistedtags_guildid",
                table: "nsfwblacklistedtags",
                column: "guildid");

            migrationBuilder.CreateIndex(
                name: "ix_permissions_guildconfigid",
                table: "permissions",
                column: "guildconfigid");

            migrationBuilder.CreateIndex(
                name: "ix_plantedcurrency_channelid",
                table: "plantedcurrency",
                column: "channelid");

            migrationBuilder.CreateIndex(
                name: "ix_plantedcurrency_messageid",
                table: "plantedcurrency",
                column: "messageid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_playlistsong_musicplaylistid",
                table: "playlistsong",
                column: "musicplaylistid");

            migrationBuilder.CreateIndex(
                name: "ix_poll_guildid",
                table: "poll",
                column: "guildid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pollanswer_pollid",
                table: "pollanswer",
                column: "pollid");

            migrationBuilder.CreateIndex(
                name: "ix_pollvote_pollid",
                table: "pollvote",
                column: "pollid");

            migrationBuilder.CreateIndex(
                name: "ix_quotes_guildid",
                table: "quotes",
                column: "guildid");

            migrationBuilder.CreateIndex(
                name: "ix_quotes_keyword",
                table: "quotes",
                column: "keyword");

            migrationBuilder.CreateIndex(
                name: "ix_reactionrole_reactionrolemessageid",
                table: "reactionrole",
                column: "reactionrolemessageid");

            migrationBuilder.CreateIndex(
                name: "ix_reactionrolemessage_guildconfigid",
                table: "reactionrolemessage",
                column: "guildconfigid");

            migrationBuilder.CreateIndex(
                name: "ix_reminders_when",
                table: "reminders",
                column: "when");

            migrationBuilder.CreateIndex(
                name: "ix_rewardedusers_patreonuserid",
                table: "rewardedusers",
                column: "patreonuserid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_selfassignableroles_guildid_roleid",
                table: "selfassignableroles",
                columns: new[] { "guildid", "roleid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shopentry_guildconfigid",
                table: "shopentry",
                column: "guildconfigid");

            migrationBuilder.CreateIndex(
                name: "ix_shopentryitem_shopentryid",
                table: "shopentryitem",
                column: "shopentryid");

            migrationBuilder.CreateIndex(
                name: "ix_slowmodeignoredrole_guildconfigid",
                table: "slowmodeignoredrole",
                column: "guildconfigid");

            migrationBuilder.CreateIndex(
                name: "ix_slowmodeignoreduser_guildconfigid",
                table: "slowmodeignoreduser",
                column: "guildconfigid");

            migrationBuilder.CreateIndex(
                name: "ix_streamroleblacklisteduser_streamrolesettingsid",
                table: "streamroleblacklisteduser",
                column: "streamrolesettingsid");

            migrationBuilder.CreateIndex(
                name: "ix_streamrolesettings_guildconfigid",
                table: "streamrolesettings",
                column: "guildconfigid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_streamrolewhitelisteduser_streamrolesettingsid",
                table: "streamrolewhitelisteduser",
                column: "streamrolesettingsid");

            migrationBuilder.CreateIndex(
                name: "ix_unbantimer_guildconfigid",
                table: "unbantimer",
                column: "guildconfigid");

            migrationBuilder.CreateIndex(
                name: "ix_unmutetimer_guildconfigid",
                table: "unmutetimer",
                column: "guildconfigid");

            migrationBuilder.CreateIndex(
                name: "ix_unroletimer_guildconfigid",
                table: "unroletimer",
                column: "guildconfigid");

            migrationBuilder.CreateIndex(
                name: "ix_userxpstats_awardedxp",
                table: "userxpstats",
                column: "awardedxp");

            migrationBuilder.CreateIndex(
                name: "ix_userxpstats_guildid",
                table: "userxpstats",
                column: "guildid");

            migrationBuilder.CreateIndex(
                name: "ix_userxpstats_userid",
                table: "userxpstats",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "ix_userxpstats_userid_guildid",
                table: "userxpstats",
                columns: new[] { "userid", "guildid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_userxpstats_xp",
                table: "userxpstats",
                column: "xp");

            migrationBuilder.CreateIndex(
                name: "ix_vcroleinfo_guildconfigid",
                table: "vcroleinfo",
                column: "guildconfigid");

            migrationBuilder.CreateIndex(
                name: "ix_waifuinfo_affinityid",
                table: "waifuinfo",
                column: "affinityid");

            migrationBuilder.CreateIndex(
                name: "ix_waifuinfo_claimerid",
                table: "waifuinfo",
                column: "claimerid");

            migrationBuilder.CreateIndex(
                name: "ix_waifuinfo_price",
                table: "waifuinfo",
                column: "price");

            migrationBuilder.CreateIndex(
                name: "ix_waifuinfo_waifuid",
                table: "waifuinfo",
                column: "waifuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_waifuitem_waifuinfoid",
                table: "waifuitem",
                column: "waifuinfoid");

            migrationBuilder.CreateIndex(
                name: "ix_waifuupdates_newid",
                table: "waifuupdates",
                column: "newid");

            migrationBuilder.CreateIndex(
                name: "ix_waifuupdates_oldid",
                table: "waifuupdates",
                column: "oldid");

            migrationBuilder.CreateIndex(
                name: "ix_waifuupdates_userid",
                table: "waifuupdates",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "ix_warningpunishment_guildconfigid",
                table: "warningpunishment",
                column: "guildconfigid");

            migrationBuilder.CreateIndex(
                name: "ix_warnings_dateadded",
                table: "warnings",
                column: "dateadded");

            migrationBuilder.CreateIndex(
                name: "ix_warnings_guildid",
                table: "warnings",
                column: "guildid");

            migrationBuilder.CreateIndex(
                name: "ix_warnings_userid",
                table: "warnings",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "ix_xpcurrencyreward_xpsettingsid",
                table: "xpcurrencyreward",
                column: "xpsettingsid");

            migrationBuilder.CreateIndex(
                name: "ix_xprolereward_xpsettingsid_level",
                table: "xprolereward",
                columns: new[] { "xpsettingsid", "level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_xpsettings_guildconfigid",
                table: "xpsettings",
                column: "guildconfigid",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_clubapplicants_clubs_clubid",
                table: "clubapplicants",
                column: "clubid",
                principalTable: "clubs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_clubapplicants_discorduser_userid",
                table: "clubapplicants",
                column: "userid",
                principalTable: "discorduser",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_clubbans_clubs_clubid",
                table: "clubbans",
                column: "clubid",
                principalTable: "clubs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_clubbans_discorduser_userid",
                table: "clubbans",
                column: "userid",
                principalTable: "discorduser",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_clubs_discorduser_ownerid",
                table: "clubs",
                column: "ownerid",
                principalTable: "discorduser",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_discorduser_clubs_clubid",
                table: "discorduser");

            migrationBuilder.DropTable(
                name: "antialtsetting");

            migrationBuilder.DropTable(
                name: "antiraidsetting");

            migrationBuilder.DropTable(
                name: "antispamignore");

            migrationBuilder.DropTable(
                name: "autocommands");

            migrationBuilder.DropTable(
                name: "autotranslateusers");

            migrationBuilder.DropTable(
                name: "bantemplates");

            migrationBuilder.DropTable(
                name: "blacklist");

            migrationBuilder.DropTable(
                name: "clubapplicants");

            migrationBuilder.DropTable(
                name: "clubbans");

            migrationBuilder.DropTable(
                name: "commandalias");

            migrationBuilder.DropTable(
                name: "commandcooldown");

            migrationBuilder.DropTable(
                name: "currencytransactions");

            migrationBuilder.DropTable(
                name: "delmsgoncmdchannel");

            migrationBuilder.DropTable(
                name: "discordpermoverrides");

            migrationBuilder.DropTable(
                name: "excludeditem");

            migrationBuilder.DropTable(
                name: "expressions");

            migrationBuilder.DropTable(
                name: "feedsub");

            migrationBuilder.DropTable(
                name: "filterchannelid");

            migrationBuilder.DropTable(
                name: "filteredword");

            migrationBuilder.DropTable(
                name: "filterlinkschannelid");

            migrationBuilder.DropTable(
                name: "filterwordschannelid");

            migrationBuilder.DropTable(
                name: "followedstream");

            migrationBuilder.DropTable(
                name: "gcchannelid");

            migrationBuilder.DropTable(
                name: "groupname");

            migrationBuilder.DropTable(
                name: "ignoredlogchannels");

            migrationBuilder.DropTable(
                name: "ignoredvoicepresencechannels");

            migrationBuilder.DropTable(
                name: "imageonlychannels");

            migrationBuilder.DropTable(
                name: "musicplayersettings");

            migrationBuilder.DropTable(
                name: "muteduserid");

            migrationBuilder.DropTable(
                name: "nsfwblacklistedtags");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "plantedcurrency");

            migrationBuilder.DropTable(
                name: "playlistsong");

            migrationBuilder.DropTable(
                name: "pollanswer");

            migrationBuilder.DropTable(
                name: "pollvote");

            migrationBuilder.DropTable(
                name: "quotes");

            migrationBuilder.DropTable(
                name: "reactionrole");

            migrationBuilder.DropTable(
                name: "reminders");

            migrationBuilder.DropTable(
                name: "repeaters");

            migrationBuilder.DropTable(
                name: "rewardedusers");

            migrationBuilder.DropTable(
                name: "rotatingstatus");

            migrationBuilder.DropTable(
                name: "selfassignableroles");

            migrationBuilder.DropTable(
                name: "shopentryitem");

            migrationBuilder.DropTable(
                name: "slowmodeignoredrole");

            migrationBuilder.DropTable(
                name: "slowmodeignoreduser");

            migrationBuilder.DropTable(
                name: "streamroleblacklisteduser");

            migrationBuilder.DropTable(
                name: "streamrolewhitelisteduser");

            migrationBuilder.DropTable(
                name: "unbantimer");

            migrationBuilder.DropTable(
                name: "unmutetimer");

            migrationBuilder.DropTable(
                name: "unroletimer");

            migrationBuilder.DropTable(
                name: "userxpstats");

            migrationBuilder.DropTable(
                name: "vcroleinfo");

            migrationBuilder.DropTable(
                name: "waifuitem");

            migrationBuilder.DropTable(
                name: "waifuupdates");

            migrationBuilder.DropTable(
                name: "warningpunishment");

            migrationBuilder.DropTable(
                name: "warnings");

            migrationBuilder.DropTable(
                name: "xpcurrencyreward");

            migrationBuilder.DropTable(
                name: "xprolereward");

            migrationBuilder.DropTable(
                name: "antispamsetting");

            migrationBuilder.DropTable(
                name: "autotranslatechannels");

            migrationBuilder.DropTable(
                name: "logsettings");

            migrationBuilder.DropTable(
                name: "musicplaylists");

            migrationBuilder.DropTable(
                name: "poll");

            migrationBuilder.DropTable(
                name: "reactionrolemessage");

            migrationBuilder.DropTable(
                name: "shopentry");

            migrationBuilder.DropTable(
                name: "streamrolesettings");

            migrationBuilder.DropTable(
                name: "waifuinfo");

            migrationBuilder.DropTable(
                name: "xpsettings");

            migrationBuilder.DropTable(
                name: "guildconfigs");

            migrationBuilder.DropTable(
                name: "clubs");

            migrationBuilder.DropTable(
                name: "discorduser");
        }
    }
}
