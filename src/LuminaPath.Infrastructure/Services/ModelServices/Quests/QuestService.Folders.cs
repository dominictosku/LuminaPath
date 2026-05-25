using LuminaPath.Core.Dtos;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.ModelServices;

public partial class QuestService
{
    public async Task<List<QuestFolderDto>> GetFoldersAsync(string userId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await LoadFolderDtos(dbContext, userId);
    }

    public async Task<Result<QuestFolderDto, FailedResult>> CreateFolderAsync(string userId, QuestFolderCreateDto dto)
    {
        var name = (dto.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return new FailedResult("Folder name is required");
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var nextSort = await dbContext.QuestFolders
            .Where(f => f.LuminaUserId == userId)
            .Select(f => (int?)f.SortOrder)
            .MaxAsync() ?? -1;

        var now = UtcNow;
        var folder = new QuestFolder
        {
            LuminaUserId = userId,
            Name = name,
            Emoji = (dto.Emoji ?? string.Empty).Trim(),
            Color = NormalizeColor(dto.Color),
            SectionName = NormalizeSection(dto.SectionName),
            SortOrder = nextSort + 1,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await dbContext.QuestFolders.AddAsync(folder);
        await dbContext.SaveChangesAsync();
        return ProjectFolderDto(folder);
    }

    public async Task<Result<QuestFolderDto, FailedResult>> UpdateFolderAsync(string userId, int folderId, QuestFolderUpdateDto dto)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var folder = await dbContext.QuestFolders
            .FirstOrDefaultAsync(f => f.Id == folderId && f.LuminaUserId == userId);

        if (folder == null)
        {
            return new FailedResult("Folder not found");
        }

        if (dto.Name is not null)
        {
            var trimmed = dto.Name.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return new FailedResult("Folder name cannot be empty");
            }
            folder.Name = trimmed;
        }

        if (dto.Emoji is not null)
        {
            folder.Emoji = dto.Emoji.Trim();
        }

        if (dto.ClearColor == true)
        {
            folder.Color = null;
        }
        else if (dto.Color is not null)
        {
            folder.Color = NormalizeColor(dto.Color);
        }

        if (dto.ClearSectionName == true)
        {
            folder.SectionName = null;
        }
        else if (dto.SectionName is not null)
        {
            folder.SectionName = NormalizeSection(dto.SectionName);
        }

        if (dto.SortOrder.HasValue)
        {
            folder.SortOrder = dto.SortOrder.Value;
        }

        folder.UpdatedAt = UtcNow;
        await dbContext.SaveChangesAsync();
        return ProjectFolderDto(folder);
    }

    public async Task<Result<int, FailedResult>> ReorderFoldersAsync(string userId, List<QuestFolderReorderItemDto> items)
    {
        if (items.Count == 0)
        {
            return 0;
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var ids = items.Select(item => item.Id).Distinct().ToList();
        var folders = await dbContext.QuestFolders
            .Where(f => f.LuminaUserId == userId && ids.Contains(f.Id))
            .ToListAsync();
        var foldersById = folders.ToDictionary(f => f.Id);

        var now = UtcNow;
        foreach (var item in items)
        {
            if (!foldersById.TryGetValue(item.Id, out var folder))
            {
                continue;
            }
            folder.SortOrder = item.SortOrder;
            folder.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync();
        return items.Count;
    }

    public async Task<Result<int, FailedResult>> DeleteFolderAsync(string userId, int folderId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var folder = await dbContext.QuestFolders
            .FirstOrDefaultAsync(f => f.Id == folderId && f.LuminaUserId == userId);

        if (folder == null)
        {
            return new FailedResult("Folder not found");
        }

        dbContext.QuestFolders.Remove(folder);
        await dbContext.SaveChangesAsync();
        return folderId;
    }

    private static async Task<List<QuestFolderDto>> LoadFolderDtos(LuminaPathDbContext dbContext, string userId)
    {
        var folders = await dbContext.QuestFolders
            .AsNoTracking()
            .Where(folder => folder.LuminaUserId == userId)
            .OrderBy(folder => folder.SortOrder)
            .ThenBy(folder => folder.Id)
            .ToListAsync();

        return folders.Select(ProjectFolderDto).ToList();
    }

    private static QuestFolderDto ProjectFolderDto(QuestFolder folder)
    {
        return new QuestFolderDto
        {
            Id = folder.Id,
            Name = folder.Name,
            Emoji = folder.Emoji,
            Color = folder.Color,
            SectionName = folder.SectionName,
            SortOrder = folder.SortOrder,
        };
    }

    private static string? NormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return null;
        }

        var trimmed = color.Trim();
        return trimmed.Length > 16 ? trimmed[..16] : trimmed;
    }

    private static string? NormalizeSection(string? section)
    {
        if (string.IsNullOrWhiteSpace(section))
        {
            return null;
        }

        var trimmed = section.Trim();
        return trimmed.Length > 48 ? trimmed[..48] : trimmed;
    }
}
