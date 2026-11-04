using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Chats.GetChatMessages;

/// <summary>Checks the size of the batch, with the same bounds as a page.</summary>
internal sealed class GetChatMessagesQueryValidator : AbstractValidator<GetChatMessagesQuery>
{
    public GetChatMessagesQueryValidator()
    {
        RuleFor(query => query.Limit)
            .InclusiveBetween(1, PageRequest.MaxPageSize)
            .WithError(ApplicationErrors.Paging.PageSizeOutOfRange);
    }
}
