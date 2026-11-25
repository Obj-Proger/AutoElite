namespace AutoElite.Application.Settings.GetWebsite;

/// <summary>Reads the address of the website from the settings of the host.</summary>
internal sealed class GetWebsiteQueryHandler(IClientLinks links) : IQueryHandler<GetWebsiteQuery, WebsiteDetails>
{
    /// <inheritdoc />
    public Task<Result<WebsiteDetails>> Handle(GetWebsiteQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(new WebsiteDetails(links.Website().AbsoluteUri)));
}
