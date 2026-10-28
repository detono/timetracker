using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class LeaveBalanceConfiguration : IEntityTypeConfiguration<LeaveBalance> {
    public void Configure(EntityTypeBuilder<LeaveBalance> builder) {
        builder.ToTable("LeaveBalances");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.AllowanceHours).HasPrecision(8, 2);
        builder.HasIndex(b => new { b.UserId, b.Year }).IsUnique();
        builder.HasOne<User>().WithMany().HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
