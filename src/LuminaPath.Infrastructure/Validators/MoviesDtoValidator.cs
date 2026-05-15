using FluentValidation;
using LuminaPath.Core.Dtos;

namespace LuminaPath.Infrastructure.Validators
{
    public class MoviesDtoValidator : AbstractValidator<MoviesDto>
    {
        public MoviesDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Title is required")
                .Length(2, 50);
        }
    }
}
