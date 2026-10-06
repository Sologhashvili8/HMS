using HMS.Domain.Entities;
using HMS.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Infrastructure.Persistence.Configurations;

public class ManagerConfiguration : IEntityTypeConfiguration<Manager>
{
    public void Configure(EntityTypeBuilder<Manager> builder)
    {
        builder.ToTable("Managers");

        
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(m => m.LastName).HasMaxLength(100).IsRequired();
        builder.Property(m => m.PersonalNumber).HasMaxLength(20).IsRequired();
        builder.Property(m => m.Email).HasMaxLength(256).IsRequired();
        builder.Property(m => m.PhoneNumber).HasMaxLength(20).IsRequired();

        builder.HasIndex(m => m.PersonalNumber).IsUnique().HasDatabaseName("UQ_Managers_PersonalNumber");
        builder.HasIndex(m => m.Email).IsUnique().HasDatabaseName("UQ_Managers_Email");

        builder.HasOne(m => m.Hotel)
            .WithMany(h => h.Managers)
            .HasForeignKey(m => m.HotelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.HotelId).HasDatabaseName("IX_Managers_HotelId");

        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<Manager>(m => m.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
