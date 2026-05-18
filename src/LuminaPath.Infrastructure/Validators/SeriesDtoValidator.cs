using FluentValidation;
using LuminaPath.Core.Dtos;
using LuminaPath.Infrastructure.Validators.Rules;

namespace LuminaPath.Infrastructure.Validators
{
    public class SeriesDtoValidator : AbstractValidator<SeriesDto>
    {
        public SeriesDtoValidator()
        {
            this.AddTitleRules(x => x.Name);
        }
    }
}
