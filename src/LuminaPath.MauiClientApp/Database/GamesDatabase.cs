using SQLite;
using LuminaPath.MauiClientApp.Models;
using LuminaPath.MauiClientApp.Constants;

namespace LuminaPath.MauiClientApp.Database
{
	public class GamesDatabase
	{
		SQLiteAsyncConnection Database;

		async Task Init()
		{
			if (Database is not null)
				return;

			Database = new SQLiteAsyncConnection(DatabaseConstant.DatabasePath, DatabaseConstant.Flags);
			var result = await Database.CreateTableAsync<Game>();
		}

		public async Task<List<Game>> GetItemsAsync()
		{
			await Init();
			return await Database.Table<Game>().ToListAsync();
		}

		public async Task<Game> GetItemAsync(int id)
		{
			await Init();
			return await Database.Table<Game>().Where(i => i.Id == id).FirstOrDefaultAsync();
		}

		public async Task<int> SaveItemAsync(Game item)
		{
			await Init();
			if (item.Id != 0)
				return await Database.UpdateAsync(item);
			else
				return await Database.InsertAsync(item);
		}

		public async Task<int> DeleteItemAsync(Game item)
		{
			await Init();
			return await Database.DeleteAsync(item);
		}
	}
}
