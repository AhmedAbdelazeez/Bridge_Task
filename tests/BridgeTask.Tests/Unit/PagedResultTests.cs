using BridgeTask.Application.Common;

namespace BridgeTask.Tests.Unit;

public class PagedResultTests
{
    [Theory]
    [InlineData(0, 1, 10, 0, false, false)]
    [InlineData(10, 1, 10, 1, false, false)]
    [InlineData(11, 1, 10, 2, false, true)]
    [InlineData(43, 3, 10, 5, true, true)]
    [InlineData(43, 5, 10, 5, true, false)]
    public void Metadata_IsCalculatedFromTotalCountAndPage(
        int totalCount, int pageNumber, int pageSize, int expectedTotalPages, bool expectedHasPrevious, bool expectedHasNext)
    {
        var result = new PagedResult<string>(Array.Empty<string>(), pageNumber, pageSize, totalCount);

        Assert.Equal(expectedTotalPages, result.TotalPages);
        Assert.Equal(expectedHasPrevious, result.HasPreviousPage);
        Assert.Equal(expectedHasNext, result.HasNextPage);
    }
}
