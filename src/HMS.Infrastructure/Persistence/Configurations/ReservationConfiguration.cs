using HMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Infrastructure.Persistence.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations", t => t.HasCheckConstraint("CK_Reservations_Dates", "[CheckOutDate] > [CheckInDate]"));

        builder.HasOne(r => r.Guest)
            .WithMany(g => g.Reservations)
            .HasForeignKey(r => r.GuestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.GuestId).HasDatabaseName("IX_Reservations_GuestId");
        builder.HasIndex(r => new { r.CheckInDate, r.CheckOutDate }).HasDatabaseName("IX_Reservations_Dates");
    }
}
