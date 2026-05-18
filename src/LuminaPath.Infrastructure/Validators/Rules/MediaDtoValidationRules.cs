using System.Linq.Expressions;
using FluentValidation;

namespace LuminaPath.Infrastructure.Validators.Rules;

internal static class MediaDtoValidationRules
{
    public static void AddTitleRules<TDto>(this AbstractValidator<TDto> validator, Expression<Func<TDto, string>> titleExpression)
    {
        validator.RuleFor(titleExpression)
            .NotEmpty().WithMessage("Title is required")
            .Length(2, 50);
    }
}
