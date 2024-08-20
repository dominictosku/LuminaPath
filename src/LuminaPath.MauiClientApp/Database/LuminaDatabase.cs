using SQLite;
using LuminaPath.MauiClientApp.Models;
using LuminaPath.MauiClientApp.Constants;
using LuminaPath.MauiClientApp.Database.Repositories;

namespace LuminaPath.MauiClientApp.Database
{
	public class LuminaDatabase
	{
		SQLiteAsyncConnection Database;

		public async Task Init()
		{
			if (Database is not null)
				return;

			Database = new SQLiteAsyncConnection(DatabaseConstant.DatabasePath, DatabaseConstant.Flags);
			await Database.CreateTableAsync<LocalDocument>();
			await Database.CreateTableAsync<LocalGame>();
			await Database.CreateTableAsync<LocalMyGame>();

			Games = new BaseRepository<LocalGame>(Database);
			MyGames = new BaseRepository<LocalMyGame>(Database);
		}

		public BaseRepository<LocalGame>? Games { get; set; }
		public BaseRepository<LocalMyGame>? MyGames { get; set; }
	}
}
