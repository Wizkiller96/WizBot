#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;

namespace WizBot.Db.Models;

// todo fix hash
public class UnmuteTimer
{
    [Key]
    public int Id { get; set; }
    
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public DateTime UnmuteAt { get; set; }
}

public class UnmuteTimerEntityConfiguration : IEntityTypeConfiguration<UnmuteTimer>
{
    public void Configure(EntityTypeBuilder<UnmuteTimer> builder)
    {
        builder.HasIndex(x => new
        {
            x.GuildId,
            x.UserId
        }).IsUnique();
    }
}