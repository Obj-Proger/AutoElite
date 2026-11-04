using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.Tests.Pagination;

public class PagedListTests
{
    [Fact]
    public void Constructor_CopiesItemsAndPaging()
    {
        var page = new PagedList<int>([1, 2], new PageRequest(2, 2), 5);

        page.Items.Should().Equal(1, 2);
        page.Page.Should().Be(2);
        page.PageSize.Should().Be(2);
        page.TotalCount.Should().Be(5);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(20, 1)]
    [InlineData(21, 2)]
    [InlineData(int.MaxValue, 107_374_183)]
    public void TotalPages_RoundsUp(int totalCount, int expectedPages)
    {
        var page = new PagedList<int>([], new PageRequest(1, 20), totalCount);

        page.TotalPages.Should().Be(expectedPages);
    }

    [Fact]
    public void FirstOfSeveralPages_HasNextButNoPreviousPage()
    {
        var page = new PagedList<int>([1, 2], new PageRequest(1, 2), 5);

        page.HasPreviousPage.Should().BeFalse();
        page.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void LastPage_HasPreviousButNoNextPage()
    {
        var page = new PagedList<int>([5], new PageRequest(3, 2), 5);

        page.HasPreviousPage.Should().BeTrue();
        page.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void Empty_HasNoItemsAndNoPages()
    {
        var page = PagedList<int>.Empty(new PageRequest(1, 20));

        page.Items.Should().BeEmpty();
        page.TotalCount.Should().Be(0);
        page.TotalPages.Should().Be(0);
        page.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void Constructor_NegativeTotal_Throws()
    {
        var act = () => new PagedList<int>([], new PageRequest(1, 20), -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_MoreItemsThanPageSize_Throws()
    {
        var act = () => new PagedList<int>([1, 2, 3], new PageRequest(1, 2), 3);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
