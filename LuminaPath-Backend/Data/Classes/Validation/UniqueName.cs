using Data.Models;
using Data.Models.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Classes.Validation
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
			LuminaPathDbContext _context = (LuminaPathDbContext)validationContext
												.GetService(typeof(LuminaPathDbContext))!;

			if (_context.Games.Any(e => e.Name == game.Name))
			{
				return new ValidationResult(GetErrorMessage());
			}

			return ValidationResult.Success;
		}
	}
}
