#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;

namespace WizBot.Db.Models;

// todo hash fix
public class UnbanTimer
{
    [Key]
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public DateTime UnbanAt { get; set; }
}

public class UnbanTimerEntityConfiguration : IEntityTypeConfiguration<UnbanTimer>
{
    public void Configure(EntityTypeBuilder<UnbanTimer> builder)
    {
        builder.HasIndex(x => new
               {
                   x.GuildId,
                   x.UserId
               })
               .IsUnique();
    }
}