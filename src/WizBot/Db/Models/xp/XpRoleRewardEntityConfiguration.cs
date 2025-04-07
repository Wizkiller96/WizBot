using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WizBot.Db.Models;

public class XpRoleRewardEntityConfiguration : IEntityTypeConfiguration<XpRoleReward>
{
    public void Configure(EntityTypeBuilder<XpRoleReward> builder)
    {
        builder.HasIndex(x => new
              {
                   x.XpSettingsId,
                   x.Level
               })
               .IsUnique();
    }
}