using FluentValidation;
using LuminaPath.Core.Dtos;

namespace LuminaPath.Infrastructure.Validators
{
    public class AnimesDtoValidator : AbstractValidator<AnimesDto>
    {
        public AnimesDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Title is required")
                .Length(2, 50);
        }
    }
}
