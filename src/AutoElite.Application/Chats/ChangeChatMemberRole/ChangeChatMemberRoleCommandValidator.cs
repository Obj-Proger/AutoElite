using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Chats.ChangeChatMemberRole;

/// <summary>Checks the role.</summary>
internal sealed class ChangeChatMemberRoleCommandValidator : AbstractValidator<ChangeChatMemberRoleCommand>
{
    public ChangeChatMemberRoleCommandValidator()
    {
        RuleFor(command => command.Role)
            .IsInEnum()
            .WithError(DomainErrors.Chat.RoleNotDefined);
    }
}
