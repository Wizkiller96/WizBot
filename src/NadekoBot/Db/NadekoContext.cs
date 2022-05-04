#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;
using NadekoBot.Db.Models;
using NadekoBot.Services.Database.Models;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace NadekoBot.Services.Database;

public abstract class NadekoContext : DbContext
{
    public DbSet<GuildConfig> GuildConfigs { get; set; }

    public DbSet<Quote> Quotes { get; set; }
    public DbSet<Reminder> Reminders { get; set; }
    public DbSet<SelfAssignedRole> SelfAssignableRoles { get; set; }
    public DbSet<MusicPlaylist> MusicPlaylists { get; set; }
    public DbSet<NadekoExpression> Expressions { get; set; }
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
    public DbSet<IgnoredVoicePresenceChannel> IgnoredVoicePresenceCHannels { get; set; }
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
    public DbSet<Poll> Poll { get; set; }
    public DbSet<WaifuInfo> WaifuInfo { get; set; }
    public DbSet<ImageOnlyChannel> ImageOnlyChannels { get; set; }
    public DbSet<NsfwBlacklistedTag> NsfwBlacklistedTags { get; set; }
    public DbSet<AutoTranslateChannel> AutoTranslateChannels { get; set; }
    public DbSet<AutoTranslateUser> AutoTranslateUsers { get; set; }

    public DbSet<Permissionv2> Permissions { get; set; }
    
    public DbSet<BankUser> BankUsers { get; set; }
    
    public DbSet<ReactionRoleV2> ReactionRoles { get; set; }

    #region Mandatory Provider-Specific Values

    protected abstract string CurrencyTransactionOtherIdDefaultValue { get; }
    protected abstract string DiscordUserLastXpGainDefaultValue { get; }
    protected abstract string LastLevelUpDefaultValue { get; }
    
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
        configEntity.HasIndex(c => c.GuildId).IsUnique();

        modelBuilder.Entity<AntiSpamSetting>().HasOne(x => x.GuildConfig).WithOne(x => x.AntiSpamSetting);

        modelBuilder.Entity<AntiRaidSetting>().HasOne(x => x.GuildConfig).WithOne(x => x.AntiRaidSetting);

        modelBuilder.Entity<GuildConfig>()
                    .HasOne(x => x.AntiAltSetting)
                    .WithOne()
                    .HasForeignKey<AntiAltSetting>(x => x.GuildConfigId)
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

        #region streamrole

        modelBuilder.Entity<StreamRoleSettings>().HasOne(x => x.GuildConfig).WithOne(x => x.StreamRole);

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

            du.Property(x => x.LastXpGain)
              .HasDefaultValueSql(DiscordUserLastXpGainDefaultValue);

            du.Property(x => x.LastLevelUp)
              .HasDefaultValueSql(LastLevelUpDefaultValue);

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

        #region PatreonRewards

        var pr = modelBuilder.Entity<RewardedUser>();
        pr.HasIndex(x => x.PatreonUserId).IsUnique();

        #endregion

        #region XpStats

        var xps = modelBuilder.Entity<UserXpStats>();
        xps.HasIndex(x => new
           {
               x.UserId,
               x.GuildId
           })
           .IsUnique();

        xps.Property(x => x.LastLevelUp)
           .HasDefaultValueSql(LastLevelUpDefaultValue);

        xps.HasIndex(x => x.UserId);
        xps.HasIndex(x => x.GuildId);
        xps.HasIndex(x => x.Xp);
        xps.HasIndex(x => x.AwardedXp);

        #endregion

        #region XpSettings

        modelBuilder.Entity<XpSettings>().HasOne(x => x.GuildConfig).WithOne(x => x.XpSettings);

        #endregion

        #region XpRoleReward

        modelBuilder.Entity<XpRoleReward>()
                    .HasIndex(x => new
                    {
                        x.XpSettingsId,
                        x.Level
                    })
                    .IsUnique();

        #endregion

        #region Club

        var ci = modelBuilder.Entity<ClubInfo>();
        ci.HasOne(x => x.Owner)
          .WithOne()
          .HasForeignKey<ClubInfo>(x => x.OwnerId)
          .OnDelete(DeleteBehavior.SetNull);

        ci.HasAlternateKey(x => new
        {
            x.Name
        });

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

        #region Polls

        modelBuilder.Entity<Poll>().HasIndex(x => x.GuildId).IsUnique();

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
            }).IsUnique();
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

        modelBuilder.Entity<NsfwBlacklistedTag>(nbt => nbt.HasIndex(x => x.GuildId).IsUnique(false));

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
        
    }

#if DEBUG
    private static readonly ILoggerFactory _debugLoggerFactory = LoggerFactory.Create(x => x.AddConsole());

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseLoggerFactory(_debugLoggerFactory);
#endif
}