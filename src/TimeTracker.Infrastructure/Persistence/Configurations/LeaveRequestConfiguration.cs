using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest> {
    public void Configure(EntityTypeBuilder<LeaveRequest> builder) {
        builder.ToTable("LeaveRequests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(r => r.Hours).HasPrecision(8, 2);
        builder.Property(r => r.Comment).HasMaxLength(1000);
        builder.Property(r => r.ReviewComment).HasMaxLength(1000);

        builder.HasIndex(r => new { r.UserId, r.StartDate });
        builder.HasIndex(r => r.Status);

        builder.HasOne<User>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.ReviewedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<HourType>().WithMany().HasForeignKey(r => r.HourTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}
