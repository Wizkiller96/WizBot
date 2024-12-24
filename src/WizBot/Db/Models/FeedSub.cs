#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;

namespace WizBot.Db.Models;

// todo hash
public class FeedSub
{
    [Key]
    public int Id { get; set; }

    public ulong GuildId { get; set; }

    public ulong ChannelId { get; set; }
    public string Url { get; set; }

    public string Message { get; set; }
}

public sealed class FeedSubEntityConfiguration : IEntityTypeConfiguration<FeedSub>
{
    public void Configure(EntityTypeBuilder<FeedSub> builder)
    {
        builder
            .HasIndex(x => new
            {
                x.GuildId,
                x.Url
            })
            .IsUnique();
    }
}