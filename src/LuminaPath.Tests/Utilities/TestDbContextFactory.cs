using LuminaPath.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Test.Utilities
{
    public sealed class TestDbContextFactory : IDbContextFactory<LuminaPathDbContext>
    {
        private readonly DbContextOptions<LuminaPathDbContext> _options;

        public TestDbContextFactory(DbContextOptions<LuminaPathDbContext> options)
        {
            _options = options;
        }

        public LuminaPathDbContext CreateDbContext()
        {
            return new LuminaPathDbContext(_options);
        }
    }
}
