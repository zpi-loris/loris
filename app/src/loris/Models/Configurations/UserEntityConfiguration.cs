using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loris.Models.Configurations;

public class UserEntityConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // primary key
        builder.HasKey(e => e.UserId);
        builder.Property(e => e.UserId).HasDefaultValueSql("uuidv7()");

        // unique index
        builder.HasIndex(e => e.DisplayName).IsUnique();
    }
}
