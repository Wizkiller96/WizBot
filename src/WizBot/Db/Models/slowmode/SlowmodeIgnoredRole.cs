using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;

namespace WizBot.Db.Models;

public class SlowmodeIgnoredRole
{
    [Key]
    public ulong Id { get; set; }
    
    public ulong RoleId { get; set; }
    public ulong GuildId { get; set; }
}

public class SlowmodeIgnoredRoleEntityConfiguration : IEntityTypeConfiguration<SlowmodeIgnoredRole>
{
    public void Configure(EntityTypeBuilder<SlowmodeIgnoredRole> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.GuildId, x.RoleId }).IsUnique();
    }
}