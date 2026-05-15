using FluentValidation;
using LuminaPath.Core.Dtos;

namespace LuminaPath.Infrastructure.Validators
{
    public class GamesDtoValidator : AbstractValidator<GamesDto>
    {
        public GamesDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Title is required")
                .Length(2, 50);
        }
    }
}
