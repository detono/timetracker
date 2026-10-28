using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Street).HasMaxLength(200);
        builder.Property(t => t.PostalCode).HasMaxLength(20);
        builder.Property(t => t.City).HasMaxLength(100);
        builder.Property(t => t.CountryCode).HasMaxLength(2);
        builder.Property(t => t.Region).HasMaxLength(10);
    }
}
