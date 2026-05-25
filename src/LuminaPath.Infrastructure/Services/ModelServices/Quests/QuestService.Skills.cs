using LuminaPath.Core.Dtos;
using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.ModelServices;

public partial class QuestService
{
    private static async Task<List<QuestSkillDto>> LoadSkillDtos(LuminaPathDbContext dbContext, string userId)
    {
        var skills = await dbContext.QuestSkills
            .AsNoTracking()
            .Include(skill => skill.Nodes)
            .Where(skill => skill.LuminaUserId == userId)
            .OrderBy(skill => skill.SortOrder)
            .ThenBy(skill => skill.Id)
            .ToListAsync();

        return skills.Select(skill => new QuestSkillDto
        {
            Id = skill.Id,
            Name = skill.Name,
            Icon = skill.Icon,
            Color = skill.Color,
            Xp = skill.Xp,
            SortOrder = skill.SortOrder,
            Nodes = skill.Nodes
                .OrderBy(node => node.SortOrder)
                .ThenBy(node => node.Id)
                .Select(node => new QuestSkillNodeDto
                {
                    Id = node.Id,
                    Name = node.Name,
                    Unlocked = node.Unlocked,
                    UnlockedAt = node.UnlockedAt,
                    SortOrder = node.SortOrder
                })
                .ToList()
        }).ToList();
    }

    private static async Task UpsertSkillsAsync(
        LuminaPathDbContext dbContext,
        string userId,
        List<QuestSkillDto> incoming,
        DateTime now)
    {
        var existing = await dbContext.QuestSkills
            .Include(skill => skill.Nodes)
            .Where(skill => skill.LuminaUserId == userId)
            .ToListAsync();
        var existingById = existing.ToDictionary(skill => skill.Id);

        var keepSkillIds = new HashSet<int>();
        foreach (var (dto, skillIndex) in incoming.Select((dto, index) => (dto, index)))
        {
            var name = (dto.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (dto.Id > 0 && existingById.TryGetValue(dto.Id, out var existingSkill))
            {
                ApplySkill(existingSkill, dto, skillIndex, name);
                UpsertNodes(dbContext, existingSkill, dto.Nodes, now);
                keepSkillIds.Add(existingSkill.Id);
            }
            else
            {
                var newSkill = new QuestSkill
                {
                    LuminaUserId = userId,
                    Nodes = []
                };
                ApplySkill(newSkill, dto, skillIndex, name);
                foreach (var (nodeDto, nodeIndex) in dto.Nodes.Select((node, index) => (node, index)))
                {
                    var nodeName = (nodeDto.Name ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(nodeName))
                    {
                        continue;
                    }

                    newSkill.Nodes.Add(BuildNewNode(nodeDto, nodeIndex, nodeName, now));
                }

                await dbContext.QuestSkills.AddAsync(newSkill);
            }
        }

        var skillsToDelete = existing.Where(skill => !keepSkillIds.Contains(skill.Id)).ToList();
        if (skillsToDelete.Count > 0)
        {
            dbContext.QuestSkills.RemoveRange(skillsToDelete);
        }
    }

    private static void ApplySkill(QuestSkill skill, QuestSkillDto dto, int fallbackSortOrder, string name)
    {
        skill.Name = name;
        skill.Icon = string.IsNullOrWhiteSpace(dto.Icon) ? "code-slash-outline" : dto.Icon;
        skill.Color = string.IsNullOrWhiteSpace(dto.Color) ? "#2563eb" : dto.Color;
        skill.Xp = Math.Max(0, dto.Xp);
        skill.SortOrder = dto.SortOrder == 0 ? fallbackSortOrder : dto.SortOrder;
    }

    private static void UpsertNodes(LuminaPathDbContext dbContext, QuestSkill skill, List<QuestSkillNodeDto> nodeDtos, DateTime now)
    {
        var nodesById = skill.Nodes.ToDictionary(node => node.Id);
        var keepNodeIds = new HashSet<int>();

        foreach (var (nodeDto, nodeIndex) in nodeDtos.Select((node, index) => (node, index)))
        {
            var nodeName = (nodeDto.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(nodeName))
            {
                continue;
            }

            if (nodeDto.Id > 0 && nodesById.TryGetValue(nodeDto.Id, out var existingNode))
            {
                existingNode.Name = nodeName;
                existingNode.Unlocked = nodeDto.Unlocked;
                existingNode.UnlockedAt = nodeDto.Unlocked ? nodeDto.UnlockedAt ?? now : null;
                existingNode.SortOrder = nodeDto.SortOrder == 0 ? nodeIndex : nodeDto.SortOrder;
                keepNodeIds.Add(existingNode.Id);
            }
            else
            {
                skill.Nodes.Add(BuildNewNode(nodeDto, nodeIndex, nodeName, now));
            }
        }

        var nodesToDelete = skill.Nodes
            .Where(node => node.Id > 0 && !keepNodeIds.Contains(node.Id))
            .ToList();
        if (nodesToDelete.Count > 0)
        {
            dbContext.QuestSkillNodes.RemoveRange(nodesToDelete);
            foreach (var node in nodesToDelete)
            {
                skill.Nodes.Remove(node);
            }
        }
    }

    private static QuestSkillNode BuildNewNode(QuestSkillNodeDto dto, int fallbackSortOrder, string name, DateTime now)
    {
        return new QuestSkillNode
        {
            Name = name,
            Unlocked = dto.Unlocked,
            UnlockedAt = dto.Unlocked ? dto.UnlockedAt ?? now : null,
            SortOrder = dto.SortOrder == 0 ? fallbackSortOrder : dto.SortOrder
        };
    }
}
