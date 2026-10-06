using HMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.Property(p => p.EncryptedCardNumber).HasMaxLength(500).IsRequired();
        builder.Property(p => p.EncryptedPhoneNumber).HasMaxLength(500).IsRequired();
        builder.Property(p => p.EncryptedPersonalNumber).HasMaxLength(500).IsRequired();

        builder.HasOne(p => p.Reservation)
            .WithOne(r => r.Payment)
            .HasForeignKey<Payment>(p => p.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.ReservationId).IsUnique();
    }
}
