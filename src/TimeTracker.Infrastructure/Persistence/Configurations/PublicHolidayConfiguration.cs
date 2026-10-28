using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class PublicHolidayConfiguration : IEntityTypeConfiguration<PublicHoliday> {
    public void Configure(EntityTypeBuilder<PublicHoliday> builder) {
        builder.ToTable("PublicHolidays");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Name).IsRequired().HasMaxLength(200);

        // One holiday per date per company.
        builder.HasIndex(h => new { h.TenantId, h.Date }).IsUnique();
    }
}
