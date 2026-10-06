using HMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Infrastructure.Persistence.Configurations;

public class HotelConfiguration : IEntityTypeConfiguration<Hotel>
{
    public void Configure(EntityTypeBuilder<Hotel> builder)
    {
        builder.ToTable("Hotels", t => t.HasCheckConstraint("CK_Hotels_Rating", "[Rating] between 1 and 5"));

        builder.Property(h => h.Name).HasMaxLength(200).IsRequired();
        builder.Property(h => h.Country).HasMaxLength(100).IsRequired();
        builder.Property(h => h.City).HasMaxLength(100).IsRequired();
        builder.Property(h => h.Address).HasMaxLength(300).IsRequired();
        builder.Property(h => h.ImageUrl).HasMaxLength(500);

        builder.HasIndex(h => new { h.Country, h.City }).HasDatabaseName("IX_Hotels_Country_City");
    }
}
