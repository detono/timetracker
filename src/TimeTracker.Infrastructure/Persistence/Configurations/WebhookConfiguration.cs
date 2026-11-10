using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class WebhookConfiguration : IEntityTypeConfiguration<Webhook> {
    public void Configure(EntityTypeBuilder<Webhook> builder) {
        builder.ToTable("Webhooks");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name).IsRequired().HasMaxLength(Webhook.MaxNameLength);
        builder.Property(w => w.Url).IsRequired().HasMaxLength(Webhook.MaxUrlLength);
        builder.Property(w => w.Events)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (a, b) => a != null && b != null && a.SequenceEqual(b),
                c => c.Aggregate(0, (h, v) => HashCode.Combine(h, v.GetHashCode())),
                c => c.ToList()));
        builder.Property(w => w.Events).HasColumnType("jsonb").IsRequired();

        // Data Protection ciphertext; no max length since its size depends on the key ring.
        builder.Property(w => w.SecretProtected).IsRequired();
        builder.Property(w => w.HeaderName).HasMaxLength(Webhook.MaxHeaderNameLength);
        builder.Property(w => w.HeaderValueProtected);
    }
}

public class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery> {
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder) {
        builder.ToTable("WebhookDeliveries");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.EventType).IsRequired().HasMaxLength(100);
        builder.Property(d => d.Summary).IsRequired().HasMaxLength(WebhookDelivery.MaxSummaryLength);
        builder.Property(d => d.Payload).IsRequired();
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.LastError).HasMaxLength(WebhookDelivery.MaxErrorLength);

        builder.HasOne<Webhook>().WithMany().HasForeignKey(d => d.WebhookId).OnDelete(DeleteBehavior.Cascade);

        // The sender looks for due ones across companies; employers read their latest.
        builder.HasIndex(d => new { d.Status, d.NextAttemptAtUtc });
        builder.HasIndex(d => new { d.TenantId, d.CreatedAtUtc });
    }
}
