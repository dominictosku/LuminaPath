using FluentValidation;
using LuminaPath.Core.Dtos;

namespace LuminaPath.Infrastructure.Validators
{
    public class SeriesDtoValidator : AbstractValidator<SeriesDto>
    {
        public SeriesDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Title is required")
                .Length(2, 50);
        }
    }
}
