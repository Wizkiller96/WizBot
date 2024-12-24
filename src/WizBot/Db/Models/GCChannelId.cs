using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;

namespace WizBot.Db.Models;

public class GCChannelId
{
    [Key]
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
}

public class GCChannelIdEntityConfiguration : IEntityTypeConfiguration<GCChannelId>
{
    public void Configure(EntityTypeBuilder<GCChannelId> builder)
    {
        builder.HasIndex(x => new
               {
                   x.GuildId,
                   x.ChannelId
               })
               .IsUnique();
    }
}