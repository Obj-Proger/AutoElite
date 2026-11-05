using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Chats.MarkChatRead;

/// <summary>Checks that the time is in UTC.</summary>
internal sealed class MarkChatReadCommandValidator : AbstractValidator<MarkChatReadCommand>
{
    public MarkChatReadCommandValidator()
    {
        RuleFor(command => command.ReadUpToUtc).MustBeUtc();
    }
}
