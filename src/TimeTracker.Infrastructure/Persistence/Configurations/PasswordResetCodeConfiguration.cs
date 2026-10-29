using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class PasswordResetCodeConfiguration : IEntityTypeConfiguration<PasswordResetCode> {
    public void Configure(EntityTypeBuilder<PasswordResetCode> builder) {
        builder.ToTable("PasswordResetCodes");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CodeHash).IsRequired();
        builder.HasIndex(c => c.UserId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
