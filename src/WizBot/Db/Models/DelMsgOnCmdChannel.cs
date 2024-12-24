using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;

namespace WizBot.Db.Models;

// todo don't save in hashset
public class DelMsgOnCmdChannel
{
    [Key]
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public bool State { get; set; }
}

public class DelMsgOnCmdChannelEntityConfiguration : IEntityTypeConfiguration<DelMsgOnCmdChannel>
{
    public void Configure(EntityTypeBuilder<DelMsgOnCmdChannel> builder)
    {
        builder.HasIndex(x => new
        {
            x.GuildId,
            x.ChannelId
        }).IsUnique();
    }
}