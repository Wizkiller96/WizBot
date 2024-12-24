using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;

namespace WizBot.Db.Models;

// todo fix usage hash
public class MutedUserId
{
    [Key]
    public ulong Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
}

public class MutedUserIdEntityConfiguration : IEntityTypeConfiguration<MutedUserId>
{
    public void Configure(EntityTypeBuilder<MutedUserId> builder)
    {
        builder.HasIndex(x => new
               {
                   x.GuildId,
                   x.UserId
               })
               .IsUnique();
    }
}