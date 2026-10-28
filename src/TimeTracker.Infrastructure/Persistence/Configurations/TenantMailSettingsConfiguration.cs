using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class TenantMailSettingsConfiguration : IEntityTypeConfiguration<TenantMailSettings> {
    public void Configure(EntityTypeBuilder<TenantMailSettings> builder) {
        builder.ToTable("TenantMailSettings");
        builder.HasKey(s => s.Id);

        // One settings record per tenant.
        builder.HasIndex(s => s.TenantId).IsUnique();

        builder.Property(s => s.Provider).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(s => s.FromAddress).HasMaxLength(320).IsRequired();
        builder.Property(s => s.FromName).HasMaxLength(200);

        builder.Property(s => s.SmtpHost).HasMaxLength(255);
        builder.Property(s => s.SmtpSecurity).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(s => s.SmtpUsername).HasMaxLength(320);

        builder.Property(s => s.GraphTenantId).HasMaxLength(255);
        builder.Property(s => s.GraphClientId).HasMaxLength(255);
        builder.Property(s => s.GraphMailbox).HasMaxLength(320);

        // Data Protection ciphertext; no max length since its size depends on the key ring.
        builder.Property(s => s.SmtpPasswordProtected);
        builder.Property(s => s.GraphClientSecretProtected);
    }
}
