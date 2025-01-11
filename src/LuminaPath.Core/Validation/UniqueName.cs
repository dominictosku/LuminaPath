using LuminaPath.Core.Dtos;
using LuminaPath.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
namespace LuminaPath.Core.Validation
{
    public class UniqueName : ValidationAttribute
    {
        public UniqueName() { }

        public string GetErrorMessage() =>
            $"Title is already registered";

        protected override ValidationResult? IsValid(
            object? value, ValidationContext validationContext)
        {
            GamesDto game = (GamesDto)validationContext.ObjectInstance;
            ILuminaPathDbContext _context = (ILuminaPathDbContext)validationContext
                                                .GetService(typeof(ILuminaPathDbContext))!;

            if (game.Id == 0 && _context.Games.Any(e => e.Name == game.Name))
            {
                return new ValidationResult(GetErrorMessage());
            }

            return ValidationResult.Success;
        }
    }
}
