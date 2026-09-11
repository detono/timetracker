using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class HourTypeConfiguration : IEntityTypeConfiguration<HourType>
{
    public void Configure(EntityTypeBuilder<HourType> builder)
    {
        builder.ToTable("HourTypes");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.Property(t => t.ColorHex).IsRequired().HasMaxLength(7);

        builder.HasIndex(t => t.Name).IsUnique();
    }
}
