#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;

namespace WizBot.Db.Models;

public class GuildXpSettings
{
    [Key]
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public bool ServerExcluded { get; set; }
}

public enum ExcludedItemType { Channel, Role }

public class XpRoleReward
{
    [Key]
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public int Level { get; set; }
    public ulong RoleId { get; set; }

    /// <summary>
    ///     Whether the role should be removed (true) or added (false)
    /// </summary>
    public bool Remove { get; set; }
}

public class XpCurrencyReward
{
    [Key]
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public int Level { get; set; }
    public int Amount { get; set; }
}

public class ExcludedItem
{
    [Key]
    public int Id { get; set; }
    public ulong GuildId { get; set; }

    public ulong ItemId { get; set; }
    public ExcludedItemType ItemType { get; set; }
}

public class XpRoleRewardEntityConfiguration : IEntityTypeConfiguration<XpRoleReward>
{
    public void Configure(EntityTypeBuilder<XpRoleReward> builder)
    {
        builder.HasIndex(x => new
        {
            x.Level,
            x.GuildId
        }).IsUnique();
    }
}

public class XpCurrencyRewardEntityConfiguration : IEntityTypeConfiguration<XpCurrencyReward>
{
    public void Configure(EntityTypeBuilder<XpCurrencyReward> builder)
    {
        builder.HasIndex(x => new
        {
            x.Level,
            x.GuildId
        }).IsUnique();
    }
}

public class ExcludedItemEntityConfiguration : IEntityTypeConfiguration<ExcludedItem>
{
    public void Configure(EntityTypeBuilder<ExcludedItem> builder)
    {
        builder.HasIndex(x => new
        {
            x.ItemId,
            x.ItemType,
            x.GuildId
        }).IsUnique();
    }
}

public class XpSettingsEntityConfiguration : IEntityTypeConfiguration<GuildXpSettings>
{
    public void Configure(EntityTypeBuilder<GuildXpSettings> builder)
    {
        builder.HasIndex(x => x.GuildId)
               .IsUnique();
    }
}