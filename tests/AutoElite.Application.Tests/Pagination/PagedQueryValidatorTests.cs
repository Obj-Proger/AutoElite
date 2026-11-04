using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.Tests.Pagination;

public class PagedQueryValidatorTests
{
    private readonly TestPagedQueryValidator _validator = new();

    [Theory]
    [InlineData(1, 1)]
    [InlineData(PageRequest.MaxPage, PageRequest.MaxPageSize)]
    public void Validate_PageAndSizeInRange_Passes(int page, int pageSize)
    {
        _validator.Validate(new TestPagedQuery(page, pageSize)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(PageRequest.MaxPage + 1)]
    public void Validate_PageOutOfRange_ReportsPageOutOfRange(int page)
    {
        var result = _validator.Validate(new TestPagedQuery(page));

        var failure = result.Errors.Should().ContainSingle().Which;
        failure.PropertyName.Should().Be("Page");
        failure.ErrorCode.Should().Be(ApplicationErrors.Paging.PageOutOfRange.Code);
        failure.ErrorMessage.Should().Be(ApplicationErrors.Paging.PageOutOfRange.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(PageRequest.MaxPageSize + 1)]
    public void Validate_PageSizeOutOfRange_ReportsPageSizeOutOfRange(int pageSize)
    {
        var result = _validator.Validate(new TestPagedQuery(1, pageSize));

        var failure = result.Errors.Should().ContainSingle().Which;
        failure.PropertyName.Should().Be("PageSize");
        failure.ErrorCode.Should().Be(ApplicationErrors.Paging.PageSizeOutOfRange.Code);
    }
}
