using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class WorkScheduleConfiguration : IEntityTypeConfiguration<WorkSchedule> {
    public void Configure(EntityTypeBuilder<WorkSchedule> builder) {
        builder.ToTable("WorkSchedules");
        builder.HasKey(s => s.Id);

        // One schedule per person, and one company default (no user) per tenant.
        builder.HasIndex(s => s.UserId).IsUnique();
        builder.HasIndex(s => s.TenantId, "IX_WorkSchedules_TenantId_CompanyDefault")
            .IsUnique().HasFilter("\"UserId\" IS NULL");
        builder.HasOne<User>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.OwnsMany(s => s.Days, day => {
            day.ToTable("WorkScheduleDays");
            day.WithOwner().HasForeignKey("WorkScheduleId");
            day.Property<int>("Id");
            day.HasKey("Id");
            day.Property(d => d.DayOfWeek).IsRequired();
            day.Property(d => d.StartTime).IsRequired();
            day.Property(d => d.EndTime).IsRequired();
            day.Property(d => d.BreakMinutes).IsRequired();
            day.Ignore(d => d.Duration);
        });
        builder.Navigation(s => s.Days).HasField("_days").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(s => s.WeeklyDuration);
        builder.Ignore(s => s.IsCompanyDefault);
    }
}
