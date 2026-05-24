using FluentValidation;
using LuminaPath.Core.Dtos;

namespace LuminaPath.Infrastructure.Validators;

public sealed class SendDirectMessageDtoValidator : AbstractValidator<SendDirectMessageDto>
{
    public SendDirectMessageDtoValidator()
    {
        RuleFor(message => message.RecipientId)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(message => message.Content)
            .NotEmpty()
            .MaximumLength(DirectMessageLimits.MaxMessageCharacters);
    }
}
