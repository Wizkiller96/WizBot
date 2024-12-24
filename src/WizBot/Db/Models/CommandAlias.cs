#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;

namespace WizBot.Db.Models;

public class CommandAlias
{
    [Key]
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public string Trigger { get; set; }
    public string Mapping { get; set; }
}

public class CommandAliasEntityConfiguration : IEntityTypeConfiguration<CommandAlias>
{
    public void Configure(EntityTypeBuilder<CommandAlias> builder)
    {
        builder.HasIndex(x => x.GuildId);
    }
}