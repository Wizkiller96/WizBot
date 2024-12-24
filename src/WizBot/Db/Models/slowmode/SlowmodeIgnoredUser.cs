using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;

namespace WizBot.Db.Models;

// todo hash
public class SlowmodeIgnoredUser
{
    [Key]
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
}

public class SlowmodeIgnoredUserEntityConfiguration : IEntityTypeConfiguration<SlowmodeIgnoredUser>
{
    public void Configure(EntityTypeBuilder<SlowmodeIgnoredUser> builder)
    {
        builder.HasIndex(x => new
               {
                   x.GuildId,
                   x.UserId
               })
               .IsUnique();
    }
}