using HMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Infrastructure.Persistence.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms", t =>
        {
            t.HasCheckConstraint("CK_Rooms_Price", "[Price] > 0");
            t.HasCheckConstraint("CK_Rooms_Capacity", "[Capacity] > 0");
        });

        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Price).HasColumnType("decimal(10,2)");

        builder.HasOne(r => r.Hotel)
            .WithMany(h => h.Rooms)
            .HasForeignKey(r => r.HotelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.HotelId).HasDatabaseName("IX_Rooms_HotelId");
        builder.HasIndex(r => r.Price).HasDatabaseName("IX_Rooms_Price");
    }
}
