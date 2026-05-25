using FluentValidation;
using LuminaPath.Infrastructure.Controllers;

namespace LuminaPath.Infrastructure.Validators;

public sealed class SteamIdentifierRequestValidator : AbstractValidator<SteamIdentifierRequest>
{
    public SteamIdentifierRequestValidator()
    {
        RuleFor(request => request.Identifier)
            .NotEmpty()
            .MaximumLength(SteamIdentifierRequest.MaxIdentifierLength);
    }
}
