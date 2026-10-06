using HMS.Domain.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Infrastructure.Persistence.Configurations;

public class RoleSeedConfiguration : IEntityTypeConfiguration<IdentityRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> builder)
    {
        builder.HasData(
            new IdentityRole<Guid>
            {
                Id = Guid.Parse("26F3816C-349C-4553-9A4C-ED31BB49105B"),
                Name = Roles.Admin,
                NormalizedName = "ADMIN",
                ConcurrencyStamp = "C6756F61-ABC2-4EC3-AF7A-F5EE3759445E"
            },
            new IdentityRole<Guid>
            {
                Id = Guid.Parse("D8CFA7E9-C1FC-448D-9702-2ADC4685C722"),
                Name = Roles.Manager,
                NormalizedName = "MANAGER",
                ConcurrencyStamp = "705A32B5-6239-41F7-8554-B63D01F73914"
            },
            new IdentityRole<Guid>
            {
                Id = Guid.Parse("C11F02AE-F2B9-4EEF-BDDF-C00DA05630B7"),
                Name = Roles.Guest,
                NormalizedName = "GUEST",
                ConcurrencyStamp = "0F264C57-F00D-41C1-8362-81C13FCC431E"
            });
    }
}
