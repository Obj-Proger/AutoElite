using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.Tests.Pagination;

public class PageRequestTests
{
    [Theory]
    [InlineData(1, 20, 0)]
    [InlineData(3, 20, 40)]
    [InlineData(PageRequest.MaxPage, PageRequest.MaxPageSize, 9_999_900)]
    public void Skip_ReturnsTheNumberOfItemsBeforeThePage(int page, int pageSize, int expectedSkip)
    {
        new PageRequest(page, pageSize).Skip.Should().Be(expectedSkip);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(PageRequest.MaxPage + 1, 20)]
    [InlineData(1, 0)]
    [InlineData(1, PageRequest.MaxPageSize + 1)]
    public void Constructor_ValueOutOfRange_Throws(int page, int pageSize)
    {
        var act = () => new PageRequest(page, pageSize);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void From_PagedQuery_CopiesPageAndSize()
    {
        var request = PageRequest.From(new TestPagedQuery(2, 50));

        request.Should().Be(new PageRequest(2, 50));
    }

    [Fact]
    public void From_QueryWithDefaults_UsesFirstPageAndDefaultSize()
    {
        var request = PageRequest.From(new TestPagedQuery());

        request.Page.Should().Be(1);
        request.PageSize.Should().Be(PageRequest.DefaultPageSize);
    }
}
