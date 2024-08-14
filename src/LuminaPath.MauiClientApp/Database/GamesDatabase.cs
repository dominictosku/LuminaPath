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
			await Database.CreateTableAsync<LocalDocument>();
			await Database.CreateTableAsync<LocalGame>();
			await Database.CreateTableAsync<LocalMyGame>();
		}

		public async Task<List<LocalGame>> GetItemsAsync()
		{
			await Init();
			return await Database.Table<LocalGame>().ToListAsync();
		}

		public async Task<LocalGame> GetItemAsync(int id)
		{
			await Init();
			return await Database.Table<LocalGame>().Where(i => i.Id == id).FirstOrDefaultAsync();
		}

		public async Task<int> SaveItemAsync(LocalGame item)
		{
			await Init();
			if (item.Id != 0)
				return await Database.UpdateAsync(item);
			else
				return await Database.InsertAsync(item);
		}

		public async Task<int> DeleteItemAsync(LocalGame item)
		{
			await Init();
			return await Database.DeleteAsync(item);
		}
	}
}
