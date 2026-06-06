using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Services.Application;
using Microsoft.EntityFrameworkCore;
using Test.Utilities;

namespace Test.Services;

public class LibraryIntegrityDiagnosticsServiceTests
{
    [Fact]
    public async Task RunAsync_CountsDuplicateExternalIdsBrokenLibraryLinksAndDocumentsMissingStorage()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        await using (var context = new LuminaPathDbContext(options))
        {
            context.MediaExternalIds.AddRange(
                new MediaExternalId { Provider = ExternalMediaProvider.Psn, ExternalId = "DUPLICATE" },
                new MediaExternalId { Provider = ExternalMediaProvider.Psn, ExternalId = "DUPLICATE" });
            context.MyGames.Add(new MyGame
            {
                LuminaUserId = "user-1",
                GameId = 999
            });
            context.MediaDocuments.Add(new MediaDocument
            {
                Name = "Broken document",
                StorageName = string.Empty
            });
            await context.SaveChangesAsync();
        }

        var service = new LibraryIntegrityDiagnosticsService(new TestDbContextFactory(options));

        var result = await service.RunAsync();

        Assert.Equal(1, result.DuplicateExternalIdGroups);
        Assert.Equal(1, result.LibraryEntriesMissingMedia);
        Assert.Equal(1, result.DocumentsMissingStorageName);
        Assert.Equal(3, result.TotalIssues);
    }
}
