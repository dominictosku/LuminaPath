using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;

namespace Test.Utilities
{
    public static class Seeding
	{
		public static List<Game> SeedGames(IEnumerable<string> names)
		{
			var list = new List<Game>();
			foreach (var name in names)
			{
				list.Add(
					new Game()
					{
						Name = name,
						Description = "Battle Royale",
						Genres = { "Shooter" },
						ReleaseDate = new DateTime(2017, 07, 28),
						Platforms = Platforms.Playstation5,
						Playtime = 100
					}
				);
			}
			return list;
		}
	}
}
