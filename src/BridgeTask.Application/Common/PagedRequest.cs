using System.ComponentModel.DataAnnotations;

namespace BridgeTask.Application.Common;

public class PagedRequest
{
    public const int MaxPageSize = 100;

    // Upper bound keeps (PageNumber - 1) * PageSize inside int range for Skip().
    [Range(1, int.MaxValue / MaxPageSize)]
    public int PageNumber { get; set; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; set; } = 10;
}
