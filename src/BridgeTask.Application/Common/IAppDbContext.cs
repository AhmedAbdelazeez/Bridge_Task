using BridgeTask.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BridgeTask.Application.Common;

public interface IAppDbContext
{
    DbSet<Country> Countries { get; }
    DbSet<City> Cities { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
