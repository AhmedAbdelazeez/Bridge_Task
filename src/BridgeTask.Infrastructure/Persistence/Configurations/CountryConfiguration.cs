using BridgeTask.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BridgeTask.Infrastructure.Persistence.Configurations;

public class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("Countries");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(Country.NameMaxLength);

        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(Country.CodeMaxLength)
            .IsUnicode(false);

        builder.HasIndex(c => c.Code)
            .IsUnique()
            .HasDatabaseName("IX_Countries_Code");

        builder.HasIndex(c => c.Name)
            .IsUnique()
            .HasDatabaseName("IX_Countries_Name");
    }
}
