using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project> {
    public void Configure(EntityTypeBuilder<Project> builder) {
        builder.Property(p => p.BudgetHours).HasPrecision(9, 2);
        builder.Property(p => p.IsBillable).HasDefaultValue(false);

        builder.OwnsMany(p => p.Members, member => {
            member.ToTable("ProjectMembers");
            member.WithOwner().HasForeignKey("ProjectId");
            member.Property(m => m.UserId).IsRequired();
            member.HasKey("ProjectId", nameof(ProjectMember.UserId));
            member.HasOne<User>().WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Navigation(p => p.Members).HasField("_members").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(p => p.IsRestricted);
    }
}
