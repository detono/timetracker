using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry> {
    public void Configure(EntityTypeBuilder<AuditEntry> builder) {
        builder.ToTable("AuditEntries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ActorName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.ActorRole).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Action).HasConversion<string>().HasMaxLength(50);
        builder.Property(e => e.SubjectName).HasMaxLength(200);

        // Read newest first per company. No foreign keys to users: the log outlives accounts.
        builder.HasIndex(e => new { e.TenantId, e.CreatedAtUtc });
    }
}
