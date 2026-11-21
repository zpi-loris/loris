using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loris.Models.Configurations;

class AutomationEntityConfiguration : IEntityTypeConfiguration<Automation>
{
    public void Configure(EntityTypeBuilder<Automation> builder)
    {
        // primary key
        builder.HasKey(e => e.AutomationId);
        builder.Property(e => e.AutomationId).ValueGeneratedNever();

        builder.Property(e => e.Triggers).HasColumnType("jsonb");
        builder.Property(e => e.Conditions).HasColumnType("jsonb");
        builder.Property(e => e.Actions).HasColumnType("jsonb");
    }
}
