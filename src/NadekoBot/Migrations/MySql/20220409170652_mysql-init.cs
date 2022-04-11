using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Migrations.Mysql
{
    public partial class mysqlinit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "autocommands",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    commandtext = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    channelid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    channelname = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    guildid = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    guildname = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    voicechannelid = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    voicechannelname = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    interval = table.Column<int>(type: "int", nullable: false),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_autocommands", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "autotranslatechannels",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guildid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    channelid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    autodelete = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_autotranslatechannels", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "bantemplates",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guildid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    text = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bantemplates", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "blacklist",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    itemid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    type = table.Column<int>(type: "int", nullable: false),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_blacklist", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "currencytransactions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    userid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    type = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    extra = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    otherid = table.Column<ulong>(type: "bigint unsigned", nullable: true, defaultValueSql: "NULL"),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_currencytransactions", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "discordpermoverrides",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    perm = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    guildid = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    command = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discordpermoverrides", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "expressions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guildid = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    response = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    trigger = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    autodeletetrigger = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    dmresponse = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    containsanywhere = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    allowtarget = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    reactions = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expressions", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "guildconfigs",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guildid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    prefix = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    deletemessageoncommand = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    autoassignroleids = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    autodeletegreetmessagestimer = table.Column<int>(type: "int", nullable: false),
                    autodeletebyemessagestimer = table.Column<int>(type: "int", nullable: false),
                    greetmessagechannelid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    byemessagechannelid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    senddmgreetmessage = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    dmgreetmessagetext = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sendchannelgreetmessage = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    channelgreetmessagetext = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sendchannelbyemessage = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    channelbyemessagetext = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    exclusiveselfassignedroles = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    autodeleteselfassignedrolemessages = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    verbosepermissions = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    permissionrole = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    filterinvites = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    filterlinks = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    filterwords = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    muterolename = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cleverbotenabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    locale = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    timezoneid = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    warningsinitialized = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    gamevoicechannel = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    verboseerrors = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    notifystreamoffline = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    warnexpirehours = table.Column<int>(type: "int", nullable: false),
                    warnexpireaction = table.Column<int>(type: "int", nullable: false),
                    sendboostmessage = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    boostmessage = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    boostmessagechannelid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    boostmessagedeleteafter = table.Column<int>(type: "int", nullable: false),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guildconfigs", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "imageonlychannels",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guildid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    channelid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_imageonlychannels", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "logsettings",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guildid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    logotherid = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    messageupdatedid = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    messagedeletedid = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    userjoinedid = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    userleftid = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    userbannedid = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    userunbannedid = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    userupdatedid = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    channelcreatedid = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    channeldestroyedid = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    channelupdatedid = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    usermutedid = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    loguserpresenceid = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    logvoicepresenceid = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    logvoicepresencettsid = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_logsettings", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "musicplayersettings",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guildid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    playerrepeat = table.Column<int>(type: "int", nullable: false),
                    musicchannelid = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    volume = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    autodisconnect = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    qualitypreset = table.Column<int>(type: "int", nullable: false),
                    autoplay = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_musicplayersettings", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "musicplaylists",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    author = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    authorid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_musicplaylists", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "nsfwblacklistedtags",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guildid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    tag = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nsfwblacklistedtags", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "plantedcurrency",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    password = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    guildid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    channelid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    userid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    messageid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plantedcurrency", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "poll",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guildid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    channelid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    question = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_poll", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "quotes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guildid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    keyword = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    authorname = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    authorid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    text = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quotes", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "reminders",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    when = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    channelid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    serverid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    userid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    message = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    isprivate = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reminders", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "repeaters",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guildid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    channelid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    lastmessageid = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    message = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    interval = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    starttimeofday = table.Column<TimeSpan>(type: "time(6)", nullable: true),
                    noredundant = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_repeaters", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "rewardedusers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    userid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    patreonuserid = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    amountrewardedthismonth = table.Column<int>(type: "int", nullable: false),
                    lastreward = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rewardedusers", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "rotatingstatus",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    status = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type = table.Column<int>(type: "int", nullable: false),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rotatingstatus", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "selfassignableroles",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guildid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    roleid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    group = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    levelrequirement = table.Column<int>(type: "int", nullable: false),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_selfassignableroles", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "userxpstats",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    userid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    guildid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    xp = table.Column<int>(type: "int", nullable: false),
                    awardedxp = table.Column<int>(type: "int", nullable: false),
                    notifyonlevelup = table.Column<int>(type: "int", nullable: false),
                    lastlevelup = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(UTC_TIMESTAMP)"),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_userxpstats", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "warnings",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guildid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    userid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    reason = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    forgiven = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    forgivenby = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    moderator = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    weight = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warnings", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "autotranslateusers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    channelid = table.Column<int>(type: "int", nullable: false),
                    userid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    source = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    target = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "antialtsetting",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guildconfigid = table.Column<int>(type: "int", nullable: false),
                    minage = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    action = table.Column<int>(type: "int", nullable: false),
                    actiondurationminutes = table.Column<int>(type: "int", nullable: false),
                    roleid = table.Column<ulong>(type: "bigint unsigned", nullable: true)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "antiraidsetting",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guildconfigid = table.Column<int>(type: "int", nullable: false),
                    userthreshold = table.Column<int>(type: "int", nullable: false),
                    seconds = table.Column<int>(type: "int", nullable: false),
                    action = table.Column<int>(type: "int", nullable: false),
                    punishduration = table.Column<int>(type: "int", nullable: false),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "antispamsetting",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guildconfigid = table.Column<int>(type: "int", nullable: false),
                    action = table.Column<int>(type: "int", nullable: false),
                    messagethreshold = table.Column<int>(type: "int", nullable: false),
                    mutetime = table.Column<int>(type: "int", nullable: false),
                    roleid = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "commandalias",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    trigger = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mapping = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    guildconfigid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_commandalias", x => x.id);
                    table.ForeignKey(
                        name: "fk_commandalias_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "commandcooldown",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    seconds = table.Column<int>(type: "int", nullable: false),
                    commandname = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    guildconfigid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_commandcooldown", x => x.id);
                    table.ForeignKey(
                        name: "fk_commandcooldown_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "delmsgoncmdchannel",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    channelid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    state = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    guildconfigid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delmsgoncmdchannel", x => x.id);
                    table.ForeignKey(
                        name: "fk_delmsgoncmdchannel_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "feedsub",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guildconfigid = table.Column<int>(type: "int", nullable: false),
                    channelid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    url = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "filterchannelid",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    channelid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    guildconfigid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filterchannelid", x => x.id);
                    table.ForeignKey(
                        name: "fk_filterchannelid_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "filteredword",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    word = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    guildconfigid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filteredword", x => x.id);
                    table.ForeignKey(
                        name: "fk_filteredword_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "filterlinkschannelid",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    channelid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    guildconfigid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filterlinkschannelid", x => x.id);
                    table.ForeignKey(
                        name: "fk_filterlinkschannelid_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "filterwordschannelid",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    channelid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    guildconfigid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filterwordschannelid", x => x.id);
                    table.ForeignKey(
                        name: "fk_filterwordschannelid_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "followedstream",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guildid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    channelid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    username = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type = table.Column<int>(type: "int", nullable: false),
                    message = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    guildconfigid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_followedstream", x => x.id);
                    table.ForeignKey(
                        name: "fk_followedstream_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "gcchannelid",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guildconfigid = table.Column<int>(type: "int", nullable: true),
                    channelid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gcchannelid", x => x.id);
                    table.ForeignKey(
                        name: "fk_gcchannelid_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "groupname",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guildconfigid = table.Column<int>(type: "int", nullable: false),
                    number = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "muteduserid",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    userid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    guildconfigid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_muteduserid", x => x.id);
                    table.ForeignKey(
                        name: "fk_muteduserid_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guildconfigid = table.Column<int>(type: "int", nullable: true),
                    index = table.Column<int>(type: "int", nullable: false),
                    primarytarget = table.Column<int>(type: "int", nullable: false),
                    primarytargetid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    secondarytarget = table.Column<int>(type: "int", nullable: false),
                    secondarytargetname = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    iscustomcommand = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    state = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_permissions_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "reactionrolemessage",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    index = table.Column<int>(type: "int", nullable: false),
                    guildconfigid = table.Column<int>(type: "int", nullable: false),
                    channelid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    messageid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    exclusive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "shopentry",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    index = table.Column<int>(type: "int", nullable: false),
                    price = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    authorid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    type = table.Column<int>(type: "int", nullable: false),
                    rolename = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    roleid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    guildconfigid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shopentry", x => x.id);
                    table.ForeignKey(
                        name: "fk_shopentry_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "slowmodeignoredrole",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    roleid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    guildconfigid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_slowmodeignoredrole", x => x.id);
                    table.ForeignKey(
                        name: "fk_slowmodeignoredrole_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "slowmodeignoreduser",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    userid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    guildconfigid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_slowmodeignoreduser", x => x.id);
                    table.ForeignKey(
                        name: "fk_slowmodeignoreduser_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "streamrolesettings",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guildconfigid = table.Column<int>(type: "int", nullable: false),
                    enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    addroleid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    fromroleid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    keyword = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "unbantimer",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    userid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    unbanat = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guildconfigid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unbantimer", x => x.id);
                    table.ForeignKey(
                        name: "fk_unbantimer_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "unmutetimer",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    userid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    unmuteat = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guildconfigid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unmutetimer", x => x.id);
                    table.ForeignKey(
                        name: "fk_unmutetimer_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "unroletimer",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    userid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    roleid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    unbanat = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guildconfigid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unroletimer", x => x.id);
                    table.ForeignKey(
                        name: "fk_unroletimer_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "vcroleinfo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    voicechannelid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    roleid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    guildconfigid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vcroleinfo", x => x.id);
                    table.ForeignKey(
                        name: "fk_vcroleinfo_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "warningpunishment",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    count = table.Column<int>(type: "int", nullable: false),
                    punishment = table.Column<int>(type: "int", nullable: false),
                    time = table.Column<int>(type: "int", nullable: false),
                    roleid = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    guildconfigid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warningpunishment", x => x.id);
                    table.ForeignKey(
                        name: "fk_warningpunishment_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "xpsettings",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guildconfigid = table.Column<int>(type: "int", nullable: false),
                    serverexcluded = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ignoredlogchannels",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    logsettingid = table.Column<int>(type: "int", nullable: false),
                    logitemid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    itemtype = table.Column<int>(type: "int", nullable: false),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ignoredvoicepresencechannels",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    logsettingid = table.Column<int>(type: "int", nullable: true),
                    channelid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ignoredvoicepresencechannels", x => x.id);
                    table.ForeignKey(
                        name: "fk_ignoredvoicepresencechannels_logsettings_logsettingid",
                        column: x => x.logsettingid,
                        principalTable: "logsettings",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "playlistsong",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    provider = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    providertype = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    uri = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    query = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    musicplaylistid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pollanswer",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    index = table.Column<int>(type: "int", nullable: false),
                    text = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pollid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pollanswer", x => x.id);
                    table.ForeignKey(
                        name: "fk_pollanswer_poll_pollid",
                        column: x => x.pollid,
                        principalTable: "poll",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pollvote",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    userid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    voteindex = table.Column<int>(type: "int", nullable: false),
                    pollid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pollvote", x => x.id);
                    table.ForeignKey(
                        name: "fk_pollvote_poll_pollid",
                        column: x => x.pollid,
                        principalTable: "poll",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "antispamignore",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    channelid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    antispamsettingid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_antispamignore", x => x.id);
                    table.ForeignKey(
                        name: "fk_antispamignore_antispamsetting_antispamsettingid",
                        column: x => x.antispamsettingid,
                        principalTable: "antispamsetting",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "reactionrole",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    emotename = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    roleid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    reactionrolemessageid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "shopentryitem",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    text = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    shopentryid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shopentryitem", x => x.id);
                    table.ForeignKey(
                        name: "fk_shopentryitem_shopentry_shopentryid",
                        column: x => x.shopentryid,
                        principalTable: "shopentry",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "streamroleblacklisteduser",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    userid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    username = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    streamrolesettingsid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_streamroleblacklisteduser", x => x.id);
                    table.ForeignKey(
                        name: "fk_streamroleblacklisteduser_streamrolesettings_streamrolesetti~",
                        column: x => x.streamrolesettingsid,
                        principalTable: "streamrolesettings",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "streamrolewhitelisteduser",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    userid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    username = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    streamrolesettingsid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_streamrolewhitelisteduser", x => x.id);
                    table.ForeignKey(
                        name: "fk_streamrolewhitelisteduser_streamrolesettings_streamrolesetti~",
                        column: x => x.streamrolesettingsid,
                        principalTable: "streamrolesettings",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "excludeditem",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    itemid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    itemtype = table.Column<int>(type: "int", nullable: false),
                    xpsettingsid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_excludeditem", x => x.id);
                    table.ForeignKey(
                        name: "fk_excludeditem_xpsettings_xpsettingsid",
                        column: x => x.xpsettingsid,
                        principalTable: "xpsettings",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "xpcurrencyreward",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    xpsettingsid = table.Column<int>(type: "int", nullable: false),
                    level = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<int>(type: "int", nullable: false),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "xprolereward",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    xpsettingsid = table.Column<int>(type: "int", nullable: false),
                    level = table.Column<int>(type: "int", nullable: false),
                    roleid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    remove = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "clubapplicants",
                columns: table => new
                {
                    clubid = table.Column<int>(type: "int", nullable: false),
                    userid = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clubapplicants", x => new { x.clubid, x.userid });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "clubbans",
                columns: table => new
                {
                    clubid = table.Column<int>(type: "int", nullable: false),
                    userid = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clubbans", x => new { x.clubid, x.userid });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "clubs",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    imageurl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    xp = table.Column<int>(type: "int", nullable: false),
                    ownerid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clubs", x => x.id);
                    table.UniqueConstraint("ak_clubs_name", x => x.name);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "discorduser",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    userid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    username = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    discriminator = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    avatarid = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    clubid = table.Column<int>(type: "int", nullable: true),
                    isclubadmin = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    totalxp = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    lastlevelup = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(UTC_TIMESTAMP)"),
                    lastxpgain = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(UTC_TIMESTAMP - INTERVAL 1 year)"),
                    notifyonlevelup = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    currencyamount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "waifuinfo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    waifuid = table.Column<int>(type: "int", nullable: false),
                    claimerid = table.Column<int>(type: "int", nullable: true),
                    affinityid = table.Column<int>(type: "int", nullable: true),
                    price = table.Column<long>(type: "bigint", nullable: false),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "waifuupdates",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    userid = table.Column<int>(type: "int", nullable: false),
                    updatetype = table.Column<int>(type: "int", nullable: false),
                    oldid = table.Column<int>(type: "int", nullable: true),
                    newid = table.Column<int>(type: "int", nullable: true),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "waifuitem",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    waifuinfoid = table.Column<int>(type: "int", nullable: true),
                    itememoji = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_waifuitem", x => x.id);
                    table.ForeignKey(
                        name: "fk_waifuitem_waifuinfo_waifuinfoid",
                        column: x => x.waifuinfoid,
                        principalTable: "waifuinfo",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
