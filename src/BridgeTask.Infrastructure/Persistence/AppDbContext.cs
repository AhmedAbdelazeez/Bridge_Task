using BridgeTask.Application.Common;
using BridgeTask.Application.Common.Exceptions;
using BridgeTask.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BridgeTask.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    private const int UniqueConstraintViolation = 2627;
    private const int UniqueIndexViolation = 2601;
    private const int ForeignKeyViolation = 547;

    private static readonly Dictionary<string, string> UniqueIndexMessages = new()
    {
        ["IX_Countries_Code"] = "A country with this code already exists.",
        ["IX_Countries_Name"] = "A country with this name already exists.",
        ["IX_Cities_CountryId_Name"] = "A city with this name already exists in this country."
    };

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Country> Countries => Set<Country>();
    public DbSet<City> Cities => Set<City>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Country>(country =>
        {
            country.ToTable("Countries");

            country.HasKey(c => c.Id);

            country.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(Country.NameMaxLength);

            country.Property(c => c.Code)
                .IsRequired()
                .HasMaxLength(Country.CodeMaxLength)
                .IsUnicode(false);

            country.HasIndex(c => c.Code)
                .IsUnique()
                .HasDatabaseName("IX_Countries_Code");

            country.HasIndex(c => c.Name)
                .IsUnique()
                .HasDatabaseName("IX_Countries_Name");
        });

        modelBuilder.Entity<City>(city =>
        {
            city.ToTable("Cities");

            city.HasKey(c => c.Id);

            city.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(City.NameMaxLength);

            // Reference data must never disappear as a side effect of deleting its parent.
            city.HasOne(c => c.Country)
                .WithMany(c => c.Cities)
                .HasForeignKey(c => c.CountryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Leading CountryId also makes this the index used for the foreign key lookups.
            city.HasIndex(c => new { c.CountryId, c.Name })
                .IsUnique()
                .HasDatabaseName("IX_Cities_CountryId_Name");
        });
    }

    // Validators check business rules first, but two concurrent requests can both pass those checks.
    // The database constraints are the final guard, so their violations are reported as conflicts too.
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: UniqueConstraintViolation or UniqueIndexViolation } sqlException)
        {
            throw new ConflictException(GetUniqueViolationMessage(sqlException.Message), ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: ForeignKeyViolation })
        {
            var message = ex.Entries.Any(e => e.State == EntityState.Deleted)
                ? "The record cannot be deleted because other records depend on it."
                : "The record refers to data that no longer exists.";

            throw new ConflictException(message, ex);
        }
    }

    private static string GetUniqueViolationMessage(string sqlMessage)
    {
        var match = UniqueIndexMessages.FirstOrDefault(m => sqlMessage.Contains(m.Key, StringComparison.Ordinal));
        return match.Value ?? "The record conflicts with existing data.";
    }
}
