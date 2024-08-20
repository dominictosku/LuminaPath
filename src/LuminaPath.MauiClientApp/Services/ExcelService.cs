using LuminaPath.MauiClientApp.Models;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminaPath.MauiClientApp.Services
{
	public class ExcelService
	{
		public MemoryStream GenerateExcel(List<LocalGame> games, List<LocalMyGame>? myGames)
		{
			// Create a new workbook
			using (var workbook = new XLWorkbook())
			{
				var carsWorksheet = workbook.AddWorksheet("Games");

				// Add headers
				carsWorksheet.Cell(1, 1).Value = "ID";
				carsWorksheet.Cell(1, 2).Value = "Name";
				carsWorksheet.Cell(1, 3).Value = "Release Date";
				carsWorksheet.Cell(1, 4).Value = "Playtime";

				// Add games data to worksheet
				for (int i = 0; i < games.Count; i++)
				{
					carsWorksheet.Cell(i + 2, 1).Value = games[i].Id;
					carsWorksheet.Cell(i + 2, 2).Value = games[i].Name;
					carsWorksheet.Cell(i + 2, 3).Value = games[i].ReleaseDate.Value.ToShortDateString();
					carsWorksheet.Cell(i + 2, 4).Value = games[i].Genre;
				}

				// Add a worksheet
				var producersWorksheet = workbook.AddWorksheet("MyGames");

				// Add headers
				producersWorksheet.Cell(1, 1).Value = "ID";
				producersWorksheet.Cell(1, 2).Value = "Name";
				if(myGames != null)
				{
					for (int i = 0; i < myGames.Count; i++)
					{
						producersWorksheet.Cell(i + 2, 1).Value = myGames[i].Id;
						producersWorksheet.Cell(i + 2, 2).Value = myGames[i].Status.ToString();
					}
				}


				// Save the workbook
				var memoryStream = new MemoryStream();
				workbook.SaveAs(memoryStream);

				memoryStream.Position = 0;
				return memoryStream;
			}
		}
	}
}
