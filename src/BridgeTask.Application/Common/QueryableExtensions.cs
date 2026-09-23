using Microsoft.EntityFrameworkCore;

namespace BridgeTask.Application.Common;

public static class QueryableExtensions
{
    // Requiring an ordered query means no caller can page over a non-deterministic order by accident.
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IOrderedQueryable<T> query,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, request.PageNumber, request.PageSize, totalCount);
    }
}
