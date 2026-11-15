using Loris.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.Mapping;

namespace Loris.Models.Configurations;

public class FunctionEntityConfiguration : IEntityTypeConfiguration<Function>
{
    public void Configure(EntityTypeBuilder<Function> builder)
    {
        // primary key
        builder.HasKey(e => new { e.DevEui, e.FunctionId });

        // NOTE:  EF core conventions opt in for VALUE GENERATION for Id ending properties
        // AND WE DON'T WANT THAT HERE
        builder.Property(e => e.FunctionId).ValueGeneratedNever();

        // relationship [p]rincipal - [d]ependant
        builder
            .HasOne(d => d.Device)
            .WithMany(p => p.Functions)
            .HasForeignKey(d => d.DevEui)
            .HasPrincipalKey(p => p.DevEui)
            .OnDelete(DeleteBehavior.ClientCascade)
            .IsRequired();
    }
}
