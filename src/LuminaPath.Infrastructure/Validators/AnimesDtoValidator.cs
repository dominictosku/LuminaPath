using FluentValidation;
using LuminaPath.Core.Dtos;
using LuminaPath.Infrastructure.Validators.Rules;

namespace LuminaPath.Infrastructure.Validators
{
    public class AnimesDtoValidator : AbstractValidator<AnimesDto>
    {
        public AnimesDtoValidator()
        {
            this.AddTitleRules(x => x.Name);
        }
    }
}
