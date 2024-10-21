#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WizBot.Db.Models;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace WizBot.Db;

public abstract class WizBotContext : DbContext
{
    public DbSet<GuildConfig> GuildConfigs { get; set; }
    public DbSet<GreetSettings> GreetSettings { get; set; }

    public DbSet<Quote> Quotes { get; set; }
    public DbSet<Reminder> Reminders { get; set; }
    public DbSet<SelfAssignedRole> SelfAssignableRoles { get; set; }
    public DbSet<MusicPlaylist> MusicPlaylists { get; set; }
    public DbSet<WizBotExpression> Expressions { get; set; }
    public DbSet<CurrencyTransaction> CurrencyTransactions { get; set; }
    public DbSet<WaifuUpdate> WaifuUpdates { get; set; }
    public DbSet<WaifuItem> WaifuItem { get; set; }
    public DbSet<Warning> Warnings { get; set; }
    public DbSet<UserXpStats> UserXpStats { get; set; }
    public DbSet<ClubInfo> Clubs { get; set; }
    public DbSet<ClubBans> ClubBans { get; set; }
    public DbSet<ClubApplicants> ClubApplicants { get; set; }


    //logging
    public DbSet<LogSetting> LogSettings { get; set; }
    public DbSet<IgnoredLogItem> IgnoredLogChannels { get; set; }

    public DbSet<RotatingPlayingStatus> RotatingStatus { get; set; }
    public DbSet<BlacklistEntry> Blacklist { get; set; }
    public DbSet<AutoCommand> AutoCommands { get; set; }
    public DbSet<RewardedUser> RewardedUsers { get; set; }
    public DbSet<PlantedCurrency> PlantedCurrency { get; set; }
    public DbSet<BanTemplate> BanTemplates { get; set; }
    public DbSet<DiscordPermOverride> DiscordPermOverrides { get; set; }
    public DbSet<DiscordUser> DiscordUser { get; set; }
    public DbSet<MusicPlayerSettings> MusicPlayerSettings { get; set; }
    public DbSet<Repeater> Repeaters { get; set; }
    public DbSet<WaifuInfo> WaifuInfo { get; set; }
    public DbSet<ImageOnlyChannel> ImageOnlyChannels { get; set; }
    public DbSet<AutoTranslateChannel> AutoTranslateChannels { get; set; }
    public DbSet<AutoTranslateUser> AutoTranslateUsers { get; set; }

    public DbSet<Permissionv2> Permissions { get; set; }

    public DbSet<BankUser> BankUsers { get; set; }

    public DbSet<ReactionRoleV2> ReactionRoles { get; set; }

    public DbSet<PatronUser> Patrons { get; set; }

    public DbSet<StreamOnlineMessage> StreamOnlineMessages { get; set; }

    public DbSet<StickyRole> StickyRoles { get; set; }

    public DbSet<TodoModel> Todos { get; set; }
    public DbSet<ArchivedTodoListModel> TodosArchive { get; set; }
    public DbSet<HoneypotChannel> HoneyPotChannels { get; set; }
    
    // public DbSet<GuildColors> GuildColors { get; set; }


    #region Mandatory Provider-Specific Values

    protected abstract string CurrencyTransactionOtherIdDefaultValue { get; }

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        #region QUOTES

        var quoteEntity = modelBuilder.Entity<Quote>();
        quoteEntity.HasIndex(x => x.GuildId);
        quoteEntity.HasIndex(x => x.Keyword);

        #endregion

        #region GuildConfig

        var configEntity = modelBuilder.Entity<GuildConfig>();

        configEntity.HasIndex(c => c.GuildId)
                    .IsUnique();

        configEntity.Property(x => x.VerboseErrors)
                    .HasDefaultValue(true);

        modelBuilder.Entity<GuildConfig>()
                    .HasMany(x => x.DelMsgOnCmdChannels)
                    .WithOne()
                    .HasForeignKey(x => x.GuildConfigId)
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GuildConfig>()
                    .HasMany(x => x.FollowedStreams)
                    .WithOne()
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GuildConfig>()
                    .HasMany(x => x.GenerateCurrencyChannelIds)
                    .WithOne(x => x.GuildConfig)
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GuildConfig>()
                    .HasMany(x => x.Permissions)
                    .WithOne()
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GuildConfig>()
                    .HasMany(x => x.CommandCooldowns)
                    .WithOne()
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GuildConfig>()
                    .HasMany(x => x.FilterInvitesChannelIds)
                    .WithOne()
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GuildConfig>()
                    .HasMany(x => x.FilterLinksChannelIds)
                    .WithOne()
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GuildConfig>()
                    .HasMany(x => x.FilteredWords)
                    .WithOne()
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GuildConfig>()
                    .HasMany(x => x.FilterWordsChannelIds)
                    .WithOne()
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GuildConfig>()
                    .HasMany(x => x.MutedUsers)
                    .WithOne()
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GuildConfig>()
                    .HasOne(x => x.AntiRaidSetting)
                    .WithOne()
                    .HasForeignKey<AntiRaidSetting>(x => x.GuildConfigId)
                    .OnDelete(DeleteBehavior.Cascade);

        // start antispam 
        
        modelBuilder.Entity<GuildConfig>()
                    .HasOne(x => x.AntiSpamSetting)
                    .WithOne()
                    .HasForeignKey<AntiSpamSetting>(x => x.GuildConfigId)
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AntiSpamSetting>()
                    .HasMany(x => x.IgnoredChannels)
                    .WithOne()
                    .OnDelete(DeleteBehavior.Cascade);

        // end antispam

        modelBuilder.Entity<GuildConfig>()
                    .HasOne(x => x.AntiAltSetting)
                    .WithOne()
                    .HasForeignKey<AntiAltSetting>(x => x.GuildConfigId)
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GuildConfig>()
                    .HasMany(x => x.UnmuteTimers)
                    .WithOne()
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GuildConfig>()
                    .HasMany(x => x.UnbanTimer)
                    .WithOne()
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GuildConfig>()
                    .HasMany(x => x.UnroleTimer)
                    .WithOne()
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GuildConfig>()
                    .HasMany(x => x.VcRoleInfos)
                    .WithOne()
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GuildConfig>()
                    .HasMany(x => x.CommandAliases)
                    .WithOne()
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GuildConfig>()
                    .HasMany(x => x.SlowmodeIgnoredRoles)
                    .WithOne()
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GuildConfig>()
                    .HasMany(x => x.SlowmodeIgnoredUsers)
                    .WithOne()
                    .OnDelete(DeleteBehavior.Cascade);

        // start shop
        modelBuilder.Entity<GuildConfig>()
                    .HasMany(x => x.ShopEntries)
                    .WithOne()
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ShopEntry>()
                    .HasMany(x => x.Items)
                    .WithOne()
                    .OnDelete(DeleteBehavior.Cascade);

        // end shop

        // start streamrole

        modelBuilder.Entity<GuildConfig>()
                    .HasOne(x => x.StreamRole)
                    .WithOne(x => x.GuildConfig)
                    .HasForeignKey<StreamRoleSettings>(x => x.GuildConfigId)
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StreamRoleSettings>()
                    .HasMany(x => x.Whitelist)
                    .WithOne(x => x.StreamRoleSettings)
                    .HasForeignKey(x => x.StreamRoleSettingsId)
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StreamRoleSettings>()
                    .HasMany(x => x.Blacklist)
                    .WithOne(x => x.StreamRoleSettings)
                    .HasForeignKey(x => x.StreamRoleSettingsId)
                    .OnDelete(DeleteBehavior.Cascade);

        // end streamrole

        modelBuilder.Entity<GuildConfig>()
                    .HasOne(x => x.XpSettings)
                    .WithOne(x => x.GuildConfig)
                    .HasForeignKey<XpSettings>(x => x.GuildConfigId)
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GuildConfig>()
                    .HasMany(x => x.FeedSubs)
                    .WithOne(x => x.GuildConfig)
                    .HasForeignKey(x => x.GuildConfigId)
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GuildConfig>()
                    .HasMany(x => x.SelfAssignableRoleGroupNames)
                    .WithOne()
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FeedSub>()
                    .HasAlternateKey(x => new
                    {
                        x.GuildConfigId,
                        x.Url
                    });

        modelBuilder.Entity<PlantedCurrency>().HasIndex(x => x.MessageId).IsUnique();

        modelBuilder.Entity<PlantedCurrency>().HasIndex(x => x.ChannelId);

        configEntity.HasIndex(x => x.WarnExpireHours).IsUnique(false);

        #endregion

        #region WarningPunishments

        var warnpunishmentEntity = modelBuilder.Entity<WarningPunishment>(b =>
        {
            b.HasAlternateKey(x => new
            {
                x.GuildId,
                x.Count
            });
        });

        #endregion

        #region Self Assignable Roles

        var selfassignableRolesEntity = modelBuilder.Entity<SelfAssignedRole>();

        selfassignableRolesEntity.HasIndex(s => new
                                 {
                                     s.GuildId,
                                     s.RoleId
                                 })
                                 .IsUnique();

        selfassignableRolesEntity.Property(x => x.Group).HasDefaultValue(0);

        #endregion

        #region MusicPlaylists

        var musicPlaylistEntity = modelBuilder.Entity<MusicPlaylist>();

        musicPlaylistEntity.HasMany(p => p.Songs).WithOne().OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region Waifus

        var wi = modelBuilder.Entity<WaifuInfo>();
        wi.HasOne(x => x.Waifu).WithOne();

        wi.HasIndex(x => x.Price);
        wi.HasIndex(x => x.ClaimerId);
        // wi.HasMany(x => x.Items)
        //     .WithOne()
        //     .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region DiscordUser

        modelBuilder.Entity<DiscordUser>(du =>
        {
            du.Property(x => x.IsClubAdmin)
              .HasDefaultValue(false);

            du.Property(x => x.NotifyOnLevelUp)
              .HasDefaultValue(XpNotificationLocation.None);

            du.Property(x => x.TotalXp)
              .HasDefaultValue(0);

            du.Property(x => x.CurrencyAmount)
              .HasDefaultValue(0);

            du.HasAlternateKey(w => w.UserId);
            du.HasOne(x => x.Club)
              .WithMany(x => x.Members)
              .IsRequired(false)
              .OnDelete(DeleteBehavior.NoAction);

            du.HasIndex(x => x.TotalXp);
            du.HasIndex(x => x.CurrencyAmount);
            du.HasIndex(x => x.UserId);
            du.HasIndex(x => x.Username);
        });

        #endregion

        #region Warnings

        modelBuilder.Entity<Warning>(warn =>
        {
            warn.HasIndex(x => x.GuildId);
            warn.HasIndex(x => x.UserId);
            warn.HasIndex(x => x.DateAdded);
            warn.Property(x => x.Weight).HasDefaultValue(1);
        });

        #endregion

        #region XpStats

        var xps = modelBuilder.Entity<UserXpStats>();
        xps.HasIndex(x => new
           {
               x.UserId,
               x.GuildId
           })
           .IsUnique();

        xps.HasIndex(x => x.UserId);
        xps.HasIndex(x => x.GuildId);
        xps.HasIndex(x => x.Xp);
        xps.HasIndex(x => x.AwardedXp);

        #endregion

        #region XpRoleReward

        modelBuilder.Entity<XpRoleReward>()
                    .HasIndex(x => new
                    {
                        x.XpSettingsId,
                        x.Level
                    })
                    .IsUnique();

        modelBuilder.Entity<XpSettings>()
                    .HasMany(x => x.RoleRewards)
                    .WithOne(x => x.XpSettings)
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<XpSettings>()
                    .HasMany(x => x.CurrencyRewards)
                    .WithOne(x => x.XpSettings)
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<XpSettings>()
                    .HasMany(x => x.ExclusionList)
                    .WithOne(x => x.XpSettings)
                    .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region Club

        var ci = modelBuilder.Entity<ClubInfo>();
        ci.HasOne(x => x.Owner)
          .WithOne()
          .HasForeignKey<ClubInfo>(x => x.OwnerId)
          .OnDelete(DeleteBehavior.SetNull);

        ci.HasIndex(x => new
          {
              x.Name
          })
          .IsUnique();

        #endregion

        #region ClubManytoMany

        modelBuilder.Entity<ClubApplicants>()
                    .HasKey(t => new
                    {
                        t.ClubId,
                        t.UserId
                    });

        modelBuilder.Entity<ClubApplicants>()
                    .HasOne(pt => pt.User)
                    .WithMany();

        modelBuilder.Entity<ClubApplicants>()
                    .HasOne(pt => pt.Club)
                    .WithMany(x => x.Applicants);

        modelBuilder.Entity<ClubBans>()
                    .HasKey(t => new
                    {
                        t.ClubId,
                        t.UserId
                    });

        modelBuilder.Entity<ClubBans>()
                    .HasOne(pt => pt.User)
                    .WithMany();

        modelBuilder.Entity<ClubBans>()
                    .HasOne(pt => pt.Club)
                    .WithMany(x => x.Bans);

        #endregion

        #region CurrencyTransactions

        modelBuilder.Entity<CurrencyTransaction>(e =>
        {
            e.HasIndex(x => x.UserId)
             .IsUnique(false);

            e.Property(x => x.OtherId)
             .HasDefaultValueSql(CurrencyTransactionOtherIdDefaultValue);

            e.Property(x => x.Type)
             .IsRequired();

            e.Property(x => x.Extra)
             .IsRequired();
        });

        #endregion

        #region Reminders

        modelBuilder.Entity<Reminder>().HasIndex(x => x.When);

        #endregion

        #region GroupName

        modelBuilder.Entity<GroupName>()
                    .HasIndex(x => new
                    {
                        x.GuildConfigId,
                        x.Number
                    })
                    .IsUnique();

        modelBuilder.Entity<GroupName>()
                    .HasOne(x => x.GuildConfig)
                    .WithMany(x => x.SelfAssignableRoleGroupNames)
                    .IsRequired();

        #endregion

        #region BanTemplate

        modelBuilder.Entity<BanTemplate>().HasIndex(x => x.GuildId).IsUnique();
        modelBuilder.Entity<BanTemplate>()
                    .Property(x => x.PruneDays)
                    .HasDefaultValue(null)
                    .IsRequired(false);

        #endregion

        #region Perm Override

        modelBuilder.Entity<DiscordPermOverride>()
                    .HasIndex(x => new
                    {
                        x.GuildId,
                        x.Command
                    })
                    .IsUnique();

        #endregion

        #region Music

        modelBuilder.Entity<MusicPlayerSettings>().HasIndex(x => x.GuildId).IsUnique();

        modelBuilder.Entity<MusicPlayerSettings>().Property(x => x.Volume).HasDefaultValue(100);

        #endregion

        #region Reaction roles

        modelBuilder.Entity<ReactionRoleV2>(rr2 =>
        {
            rr2.HasIndex(x => x.GuildId)
               .IsUnique(false);

            rr2.HasIndex(x => new
               {
                   x.MessageId,
                   x.Emote
               })
               .IsUnique();
        });

        #endregion

        #region LogSettings

        modelBuilder.Entity<LogSetting>(ls => ls.HasIndex(x => x.GuildId).IsUnique());

        modelBuilder.Entity<LogSetting>(ls => ls
                                              .HasMany(x => x.LogIgnores)
                                              .WithOne(x => x.LogSetting)
                                              .OnDelete(DeleteBehavior.Cascade));

        modelBuilder.Entity<IgnoredLogItem>(ili => ili
                                                   .HasIndex(x => new
                                                   {
                                                       x.LogSettingId,
                                                       x.LogItemId,
                                                       x.ItemType
                                                   })
                                                   .IsUnique());

        #endregion

        modelBuilder.Entity<ImageOnlyChannel>(ioc => ioc.HasIndex(x => x.ChannelId).IsUnique());

        var atch = modelBuilder.Entity<AutoTranslateChannel>();
        atch.HasIndex(x => x.GuildId).IsUnique(false);

        atch.HasIndex(x => x.ChannelId).IsUnique();

        atch.HasMany(x => x.Users).WithOne(x => x.Channel).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AutoTranslateUser>(atu => atu.HasAlternateKey(x => new
        {
            x.ChannelId,
            x.UserId
        }));

        #region BANK

        modelBuilder.Entity<BankUser>(bu => bu.HasIndex(x => x.UserId).IsUnique());

        #endregion


        #region Patron

        // currency rewards
        var pr = modelBuilder.Entity<RewardedUser>();
        pr.HasIndex(x => x.PlatformUserId).IsUnique();

        // patrons
        // patrons are not identified by their user id, but by their platform user id
        // as multiple accounts (even maybe on different platforms) could have
        // the same account connected to them
        modelBuilder.Entity<PatronUser>(pu =>
        {
            pu.HasIndex(x => x.UniquePlatformUserId).IsUnique();
            pu.HasKey(x => x.UserId);
        });

        // quotes are per user id

        #endregion

        #region Xp Item Shop

        modelBuilder.Entity<XpShopOwnedItem>(
            x =>
            {
                // user can own only one of each item
                x.HasIndex(model => new
                 {
                     model.UserId,
                     model.ItemType,
                     model.ItemKey
                 })
                 .IsUnique();
            });

        #endregion

        #region AutoPublish

        modelBuilder.Entity<AutoPublishChannel>(apc => apc
                                                       .HasIndex(x => x.GuildId)
                                                       .IsUnique());

        #endregion

        #region GamblingStats

        modelBuilder.Entity<GamblingStats>(gs => gs
                                                 .HasIndex(x => x.Feature)
                                                 .IsUnique());

        #endregion

        #region Sticky Roles

        modelBuilder.Entity<StickyRole>(sr => sr.HasIndex(x => new
                                                {
                                                    x.GuildId,
                                                    x.UserId
                                                })
                                                .IsUnique());

        #endregion


        #region Giveaway

        modelBuilder.Entity<GiveawayModel>()
                    .HasMany(x => x.Participants)
                    .WithOne()
                    .HasForeignKey(x => x.GiveawayId)
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GiveawayUser>(gu => gu
                                                .HasIndex(x => new
                                                {
                                                    x.GiveawayId,
                                                    x.UserId
                                                })
                                                .IsUnique());

        #endregion

        #region Todo

        modelBuilder.Entity<TodoModel>()
                    .HasKey(x => x.Id);

        modelBuilder.Entity<TodoModel>()
                    .HasIndex(x => x.UserId)
                    .IsUnique(false);

        modelBuilder.Entity<ArchivedTodoListModel>()
                    .HasMany(x => x.Items)
                    .WithOne()
                    .HasForeignKey(x => x.ArchiveId)
                    .OnDelete(DeleteBehavior.Cascade);

        #endregion
        
        #region GreetSettings

        modelBuilder
            .Entity<GreetSettings>(gs => gs.HasIndex(x => new
                                           {
                                               x.GuildId,
                                               x.GreetType
                                           })
                                           .IsUnique());

        modelBuilder.Entity<GreetSettings>(gs =>
        {
            gs
                .Property(x => x.IsEnabled)
                .HasDefaultValue(false);

            gs
                .Property(x => x.AutoDeleteTimer)
                .HasDefaultValue(0);
        });

        #endregion
    }

#if DEBUG
    private static readonly ILoggerFactory _debugLoggerFactory = LoggerFactory.Create(x => x.AddConsole());

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseLoggerFactory(_debugLoggerFactory);
#endif
}