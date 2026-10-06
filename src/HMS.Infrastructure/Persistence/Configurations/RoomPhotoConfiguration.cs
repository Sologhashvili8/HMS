using HMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Infrastructure.Persistence.Configurations;

public class RoomPhotoConfiguration : IEntityTypeConfiguration<RoomPhoto>
{
    public void Configure(EntityTypeBuilder<RoomPhoto> builder)
    {
        builder.ToTable("RoomPhotos");

        builder.Property(p => p.Url).HasMaxLength(500).IsRequired();

        builder.HasOne(p => p.Room)
            .WithMany(r => r.RoomPhotos)
            .HasForeignKey(p => p.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.RoomId);
    }
}
