using BridgeTask.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BridgeTask.Infrastructure.Persistence.Configurations;

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("Cities");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(City.NameMaxLength);

        // Reference data must never disappear as a side effect of deleting its parent.
        builder.HasOne(c => c.Country)
            .WithMany(c => c.Cities)
            .HasForeignKey(c => c.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Leading CountryId also makes this the index used for the foreign key lookups.
        builder.HasIndex(c => new { c.CountryId, c.Name })
            .IsUnique()
            .HasDatabaseName("IX_Cities_CountryId_Name");
    }
}
