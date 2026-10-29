using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class LockedMonthConfiguration : IEntityTypeConfiguration<LockedMonth> {
    public void Configure(EntityTypeBuilder<LockedMonth> builder) {
        builder.ToTable("LockedMonths");
        builder.HasKey(m => m.Id);
        builder.Ignore(m => m.FirstDay);
        builder.Ignore(m => m.LastDay);

        // A month is locked at most once per company.
        builder.HasIndex(m => new { m.TenantId, m.Year, m.Month }).IsUnique();
    }
}
