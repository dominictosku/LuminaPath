using FluentValidation;
using LuminaPath.Core.Dtos;
using LuminaPath.Infrastructure.Validators.Rules;

namespace LuminaPath.Infrastructure.Validators
{
    public class GamesDtoValidator : AbstractValidator<GamesDto>
    {
        public GamesDtoValidator()
        {
            this.AddTitleRules(x => x.Name);
        }
    }
}
