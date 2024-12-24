#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;

namespace WizBot.Db.Models;

// todo remove unroletimer in favor of temprole
public class UnroleTimer
{
    [Key]
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public ulong RoleId { get; set; }
    public DateTime UnbanAt { get; set; }
}

public class UnroleTimerEntityConfiguration : IEntityTypeConfiguration<UnroleTimer>
{
    public void Configure(EntityTypeBuilder<UnroleTimer> builder)
    {
        builder.HasIndex(x => new
               {
                   x.GuildId,
                   x.UserId
               })
               .IsUnique();
    }
}