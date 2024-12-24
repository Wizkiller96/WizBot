#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;

namespace WizBot.Db.Models;

public enum ShopEntryType
{
    Role,
    List,
    Command
}

public class ShopEntry : IIndexed
{
    [Key]
    public int Id { get; set; }

    public ulong GuildId { get; set; }

    public int Index { get; set; }
    public int Price { get; set; }
    public string Name { get; set; }
    public ulong AuthorId { get; set; }

    public ShopEntryType Type { get; set; }

    //role
    public string RoleName { get; set; }
    public ulong RoleId { get; set; }

    //list
    public HashSet<ShopEntryItem> Items { get; set; } = new();
    public ulong? RoleRequirement { get; set; }

    // command 
    public string Command { get; set; }
}

// todo check if this hash is needed
public class ShopEntryItem : DbEntity
{
    public string Text { get; set; }

    public override bool Equals(object obj)
    {
        if (obj is null || GetType() != obj.GetType())
            return false;
        return ((ShopEntryItem)obj).Text == Text;
    }

    public override int GetHashCode()
        => Text.GetHashCode(StringComparison.InvariantCulture);
}

public class ShopEntryEntityConfiguration : IEntityTypeConfiguration<ShopEntry>
{
    public void Configure(EntityTypeBuilder<ShopEntry> builder)
    {
        builder.HasIndex(x => new
               {
                   x.GuildId,
                   x.Index
               })
               .IsUnique();

        builder
            .HasMany(x => x.Items)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}