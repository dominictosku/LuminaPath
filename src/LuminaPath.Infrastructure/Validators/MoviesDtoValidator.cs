using FluentValidation;
using LuminaPath.Core.Dtos;
using LuminaPath.Infrastructure.Validators.Rules;

namespace LuminaPath.Infrastructure.Validators
{
    public class MoviesDtoValidator : AbstractValidator<MoviesDto>
    {
        public MoviesDtoValidator()
        {
            this.AddTitleRules(x => x.Name);
        }
    }
}
