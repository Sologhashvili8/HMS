using HMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Infrastructure.Persistence.Configurations;

public class AmenityConfiguration : IEntityTypeConfiguration<Amenity>
{
    public void Configure(EntityTypeBuilder<Amenity> builder)
    {
        builder.ToTable("Amenities");

        builder.Property(a => a.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(a => a.Name).IsUnique();

        builder.HasData(
            new Amenity { Id = 1, Name = "WiFi" },
            new Amenity { Id = 2, Name = "TV" },
            new Amenity { Id = 3, Name = "Air conditioning" },
            new Amenity { Id = 4, Name = "Balcony" },
            new Amenity { Id = 5, Name = "Mini bar" },
            new Amenity { Id = 6, Name = "Sea view" }
        );
    }
}
