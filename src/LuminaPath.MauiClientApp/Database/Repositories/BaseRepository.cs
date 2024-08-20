using LuminaPath.Core.Common.Interfaces;
using LuminaPath.MauiClientApp.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminaPath.MauiClientApp.Database.Repositories
{
	public class BaseRepository<TItem> where TItem : IBasicInfo, new()
	{
		SQLiteAsyncConnection Database;

		public BaseRepository(SQLiteAsyncConnection database) 
		{
			Database = database;
		}
		public virtual async Task<List<TItem>> GetItemsAsync()
		{
			return await Database.Table<TItem>().ToListAsync();
		}

		public virtual async Task<TItem> GetItemAsync(int id)
		{
			return await Database.Table<TItem>().Where(i => i.Id == id).FirstOrDefaultAsync();
		}

		public virtual async Task<int> SaveItemAsync(TItem item)
		{
			if (item.Id != 0)
				return await Database.UpdateAsync(item);
			else
				return await Database.InsertAsync(item);
		}

		public virtual async Task<int> DeleteItemAsync(TItem item)
		{
			return await Database.DeleteAsync(item);
		}
	}
}
