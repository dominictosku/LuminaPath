using Domain.Common.Interfaces;
using Domain.Models.Gaming;
using System.ComponentModel.DataAnnotations;
namespace Domain.Common.Validation
{
	public class UniqueName : ValidationAttribute
	{
		public UniqueName() { }

		public string GetErrorMessage() =>
			$"Title is already registered";

		protected override ValidationResult? IsValid(
			object? value, ValidationContext validationContext)
		{
			Game game = (Game)validationContext.ObjectInstance;
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
