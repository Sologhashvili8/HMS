using HMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Infrastructure.Persistence.Configurations;

public class ReservationRoomConfiguration : IEntityTypeConfiguration<ReservationRoom>
{
    public void Configure(EntityTypeBuilder<ReservationRoom> builder)
    {
        builder.ToTable("ReservationRooms");

        builder.HasKey(rr => new { rr.ReservationId, rr.RoomId });

        builder.HasOne(rr => rr.Reservation)
            .WithMany(r => r.ReservationRooms)
            .HasForeignKey(rr => rr.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rr => rr.Room)
            .WithMany(r => r.ReservationRooms)
            .HasForeignKey(rr => rr.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(rr => rr.RoomId).HasDatabaseName("IX_ReservationRooms_RoomId");
    }
}
