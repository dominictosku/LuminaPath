using Data;
using Data.Models;
using Data.Repositories;
using LuminaPath.Helper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Utilities
{
	public static class DbContext
	{
		public static DbContextOptions<LuminaPathDbContext> TestDbContextOptions()
		{
			// Create a new service provider to create a new in-memory database.
			var serviceProvider = new ServiceCollection()
				.AddEntityFrameworkInMemoryDatabase()
				.AddAutoMapper(typeof(AutoMapperProfile))
				.BuildServiceProvider();

			// Create a new options instance using an in-memory database and 
			// IServiceProvider that the context should resolve all of its 
			// services from.
			var builder = new DbContextOptionsBuilder<LuminaPathDbContext>()
				.UseInMemoryDatabase("InMemoryDb")
				.UseInternalServiceProvider(serviceProvider);

			return builder.Options;
		}
	}
}
