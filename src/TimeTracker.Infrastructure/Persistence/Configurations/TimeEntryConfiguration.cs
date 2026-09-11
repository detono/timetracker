using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> builder)
    {
        builder.ToTable("TimeEntries");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.WorkDate).IsRequired();
        builder.Property(t => t.StartTime).IsRequired();
        builder.Property(t => t.EndTime).IsRequired();
        builder.Property(t => t.BreakMinutes).IsRequired();
        builder.Property(t => t.Notes).HasMaxLength(1000);

        builder.HasIndex(t => new { t.UserId, t.WorkDate });

        builder.HasOne<HourType>()
            .WithMany()
            .HasForeignKey(t => t.HourTypeId)
            .OnDelete(DeleteBehavior.Restrict); // hour types are deactivated, never deleted, once in use

        builder.Ignore(t => t.Duration);
    }
}
