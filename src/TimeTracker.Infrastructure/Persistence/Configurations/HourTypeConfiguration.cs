using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class HourTypeConfiguration : IEntityTypeConfiguration<HourType> {
    public void Configure(EntityTypeBuilder<HourType> builder) {
        builder.ToTable("HourTypes");
        builder.HasKey(t => t.Id);

        var dictionaryComparer = new ValueComparer<Dictionary<string, string>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToDictionary(k => k.Key, v => v.Value)
        );
        
        builder.Property(h => h.LocalizedNames)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) 
                     ?? new Dictionary<string, string>()
            )
            .Metadata.SetValueComparer(dictionaryComparer); 
        
        builder.Property(h => h.LocalizedNames)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(t => t.ColorHex).IsRequired().HasMaxLength(7);

        builder.Property(t => t.IsDefault).HasDefaultValue(false);
    }
}