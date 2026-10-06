using HMS.Domain.Entities;
using HMS.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Infrastructure.Persistence.Configurations;

public class GuestConfiguration : IEntityTypeConfiguration<Guest>
{
    public void Configure(EntityTypeBuilder<Guest> builder)
    {
        builder.ToTable("Guests");

        builder.Property(g => g.Id).ValueGeneratedNever();

        builder.Property(g => g.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(g => g.LastName).HasMaxLength(100).IsRequired();
        builder.Property(g => g.PersonalNumber).HasMaxLength(20).IsRequired();
        builder.Property(g => g.PhoneNumber).HasMaxLength(20).IsRequired();

        builder.HasIndex(g => g.PersonalNumber).IsUnique().HasDatabaseName("UQ_Guests_PersonalNumber");
        builder.HasIndex(g => g.PhoneNumber).IsUnique().HasDatabaseName("UQ_Guests_PhoneNumber");

        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<Guest>(g => g.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
