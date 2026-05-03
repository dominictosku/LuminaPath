using LuminaPath.Core;
using LuminaPath.Core.Mapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LuminaPath.Infrastructure;

namespace Test.Utilities
{
	public static class DbContext
	{
		public static DbContextOptions<LuminaPathDbContext> TestDbContextOptions()
		{
			// Create a new service provider to create a new in-memory database.
			var serviceProvider = new ServiceCollection()
				.AddEntityFrameworkInMemoryDatabase()
				.AddScoped<IObjectMapper, ObjectMapper>()
				.BuildServiceProvider();

			// Create a new options instance using an in-memory database and 
			// IServiceProvider that the context should resolve all of its 
			// services from.
			var builder = new DbContextOptionsBuilder<LuminaPathDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.UseInternalServiceProvider(serviceProvider);

			return builder.Options;
		}
	}
}
