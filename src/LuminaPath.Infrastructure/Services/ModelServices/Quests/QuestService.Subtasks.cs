using LuminaPath.Core.Dtos;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.ModelServices;

public partial class QuestService
{
    public async Task<Result<QuestMutationResultDto, FailedResult>> AddSubtaskAsync(string userId, int questId, QuestSubtaskCreateDto dto)
    {
        var title = (dto.Title ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            return new FailedResult("Subtask title is required");
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var quest = await dbContext.Quests
            .Include(q => q.Subtasks)
            .FirstOrDefaultAsync(q => q.Id == questId && q.LuminaUserId == userId);
        if (quest == null)
        {
            return new FailedResult("Quest not found");
        }

        var nextSort = quest.Subtasks.Count == 0 ? 0 : quest.Subtasks.Max(s => s.SortOrder) + 1;
        var now = UtcNow;
        var subtask = new QuestSubtask
        {
            QuestId = quest.Id,
            Title = title,
            SortOrder = nextSort,
            CreatedAt = now
        };
        quest.Subtasks.Add(subtask);
        quest.UpdatedAt = now;
        await dbContext.SaveChangesAsync();

        return await BuildMutationResultAsync(dbContext, userId, quest.Id);
    }

    public async Task<Result<QuestMutationResultDto, FailedResult>> UpdateSubtaskAsync(
        string userId,
        int questId,
        int subtaskId,
        QuestSubtaskUpdateDto dto)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var quest = await dbContext.Quests
            .Include(q => q.Subtasks)
            .FirstOrDefaultAsync(q => q.Id == questId && q.LuminaUserId == userId);
        if (quest == null)
        {
            return new FailedResult("Quest not found");
        }

        var subtask = quest.Subtasks.FirstOrDefault(s => s.Id == subtaskId);
        if (subtask == null)
        {
            return new FailedResult("Subtask not found");
        }

        if (dto.Title is not null)
        {
            var trimmed = dto.Title.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return new FailedResult("Subtask title cannot be empty");
            }

            subtask.Title = trimmed;
        }

        if (dto.Completed.HasValue)
        {
            var now = UtcNow;
            if (dto.Completed.Value && !subtask.Completed)
            {
                subtask.Completed = true;
                subtask.CompletedAt = now;
            }
            else if (!dto.Completed.Value && subtask.Completed)
            {
                subtask.Completed = false;
                subtask.CompletedAt = null;
            }
        }

        if (dto.SortOrder.HasValue)
        {
            subtask.SortOrder = dto.SortOrder.Value;
        }

        quest.UpdatedAt = UtcNow;
        await dbContext.SaveChangesAsync();

        return await BuildMutationResultAsync(dbContext, userId, quest.Id);
    }

    public async Task<Result<int, FailedResult>> DeleteSubtaskAsync(string userId, int questId, int subtaskId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var subtask = await dbContext.QuestSubtasks
            .Include(s => s.Quest)
            .FirstOrDefaultAsync(s => s.Id == subtaskId && s.QuestId == questId && s.Quest!.LuminaUserId == userId);
        if (subtask == null)
        {
            return new FailedResult("Subtask not found");
        }

        dbContext.QuestSubtasks.Remove(subtask);
        await dbContext.SaveChangesAsync();
        return subtaskId;
    }
}
