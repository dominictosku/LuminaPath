using LuminaPath.Core.Dtos;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace LuminaPath.Features.Pages;

public partial class Quests
{
    private enum PageMode
    {
        Quests,
        Skills
    }

    private enum QuestFilter
    {
        Today,
        Upcoming,
        Inbox,
        All
    }

    private QuestBoardDto _board = new();
    private PageMode _mode = PageMode.Quests;
    private QuestFilter _filter = QuestFilter.Today;
    private bool _loading = true;
    private string _quickAddTitle = string.Empty;
    private QuestType _quickAddType = QuestType.Sub;
    private QuestPriority _quickAddPriority = QuestPriority.Medium;
    private QuestRecurrence _quickAddRecurrence = QuestRecurrence.None;
    private DateTime? _quickAddDue;
    private int? _quickAddGameId;
    private int? _quickAddSkillId;
    private bool _quickAddAdvancedOpen;
    private string _searchQuery = string.Empty;
    private string? _tagFilter;
    private int? _expandedQuestId;
    private QuestEditDraft? _editDraft;
    private readonly Dictionary<int, string> _subtaskDrafts = new();
    private List<MyGame> _library = [];
    private string _newSkillName = string.Empty;
    private string _newSkillIcon = Icons.Material.Filled.Code;
    private string _newSkillColor = "#2563eb";
    private string _newNodeName = string.Empty;
    private int? _editingSkillId;
    private int _temporaryId = -1;

    private int Level => Math.Max(1, (_board.Xp / 200) + 1);
    private int XpIntoLevel => _board.Xp % 200;
    private int XpProgress => XpIntoLevel * 100 / 200;
    private string Title => Level >= 15 ? "Legend" : Level >= 10 ? "Master" : Level >= 6 ? "Adept" : Level >= 3 ? "Apprentice" : "Initiate";
    private int ActiveQuestCount => _board.Quests.Count(quest => !quest.Completed);
    private int CompletedQuestCount => _board.Quests.Count(quest => quest.Completed);
    private int UnlockedNodeCount => _board.Skills.Sum(skill => skill.Nodes.Count(node => node.Unlocked));
    private int TodayCount => _board.Quests.Count(quest => !quest.Completed && IsToday(quest.DueDate));
    private int OverdueCount => _board.Quests.Count(quest => !quest.Completed && IsOverdue(quest.DueDate));
    private bool IsQuickAddDueToday => IsToday(_quickAddDue);
    private bool IsQuickAddDueTomorrow => IsSameDay(_quickAddDue, DateTime.Today.AddDays(1));
    private QuestDto? NextQueuedQuest => _board.Quests
        .Where(quest => !quest.Completed && !IsToday(quest.DueDate) && !IsOverdue(quest.DueDate))
        .OrderBy(quest => quest.DueDate is null)
        .ThenBy(quest => quest.DueDate)
        .ThenByDescending(quest => quest.Priority)
        .ThenBy(quest => quest.SortOrder)
        .FirstOrDefault();
    private List<string> AvailableTags => _board.Quests
        .SelectMany(quest => quest.Tags)
        .Where(tag => !string.IsNullOrWhiteSpace(tag))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(tag => tag)
        .ToList();
    private List<QuestDto> VisibleQuests => ApplySearchAndTag(FilterBaseQuests(_filter)).ToList();
    private List<QuestSection> QuestSections => BuildQuestSections();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if (!string.IsNullOrWhiteSpace(UserId))
        {
            _board = await QuestService.GetBoardAsync(UserId);
            _library = await MyGameService.GetMyMedia(UserId);
        }

        _loading = false;
    }

    private async Task OnQuickAddKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Enter")
        {
            await AddQuest();
        }
    }

    private async Task AddQuest()
    {
        var title = _quickAddTitle.Trim();
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(UserId))
        {
            return;
        }

        var result = await QuestService.CreateAsync(UserId, new QuestCreateDto
        {
            Title = title,
            Type = _quickAddType,
            Priority = _quickAddPriority,
            Recurrence = _quickAddRecurrence,
            DueDate = _quickAddDue,
            MyGameId = _quickAddGameId,
            SkillId = _quickAddSkillId
        });

        result.Match(
            mutation =>
            {
                _board.Quests.Insert(0, mutation.Quest);
                ApplyMutationMeta(mutation);
                _quickAddTitle = string.Empty;
                Snackbar.Add("Quest added", Severity.Success);
                return 0;
            },
            failed =>
            {
                Snackbar.Add(string.Join("; ", failed.errorMessage), Severity.Error);
                return 0;
            });
    }

    private void SetQuickAddToday() => _quickAddDue = DateTime.Today;
    private void SetQuickAddTomorrow() => _quickAddDue = DateTime.Today.AddDays(1);
    private void ResetQuickAddDue() => _quickAddDue = null;

    private async Task ToggleQuest(QuestDto quest)
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            return;
        }

        var newCompleted = !quest.Completed;
        var result = await QuestService.UpdateAsync(UserId, quest.Id, new QuestUpdateDto
        {
            Completed = newCompleted
        });

        result.Match(
            mutation =>
            {
                ApplyMutation(quest.Id, mutation.Quest);
                if (mutation.SpawnedQuest is not null)
                {
                    _board.Quests.Insert(0, mutation.SpawnedQuest);
                }
                ApplyMutationMeta(mutation);
                Snackbar.Add(newCompleted ? CompletionMessage(mutation) : "Quest reopened", Severity.Success);
                return 0;
            },
            failed =>
            {
                Snackbar.Add(string.Join("; ", failed.errorMessage), Severity.Error);
                return 0;
            });
    }

    private void ToggleExpand(QuestDto quest)
    {
        if (_expandedQuestId == quest.Id)
        {
            CancelEdit();
            return;
        }

        _expandedQuestId = quest.Id;
        _editDraft = QuestEditDraft.From(quest);
    }

    private void CancelEdit()
    {
        _expandedQuestId = null;
        _editDraft = null;
    }

    private async Task SaveEdit(QuestDto quest)
    {
        if (_editDraft is null || string.IsNullOrWhiteSpace(UserId))
        {
            return;
        }

        var result = await QuestService.UpdateAsync(UserId, quest.Id, new QuestUpdateDto
        {
            Title = _editDraft.Title,
            Notes = _editDraft.Notes,
            Type = _editDraft.Type,
            Priority = _editDraft.Priority,
            Recurrence = _editDraft.Recurrence,
            DueDate = _editDraft.DueDate,
            ClearDueDate = _editDraft.DueDate is null,
            Tags = ParseTags(_editDraft.Tags),
            MyGameId = _editDraft.MyGameId,
            ClearMyGame = _editDraft.MyGameId is null,
            SkillId = _editDraft.SkillId,
            ClearSkill = _editDraft.SkillId is null
        });

        result.Match(
            mutation =>
            {
                ApplyMutation(quest.Id, mutation.Quest);
                ApplyMutationMeta(mutation);
                CancelEdit();
                Snackbar.Add("Quest updated", Severity.Success);
                return 0;
            },
            failed =>
            {
                Snackbar.Add(string.Join("; ", failed.errorMessage), Severity.Error);
                return 0;
            });
    }

    private Task ScheduleToday(QuestDto quest) => UpdateQuestDate(quest, DateTime.Today, "Scheduled for today");
    private Task ScheduleTomorrow(QuestDto quest) => UpdateQuestDate(quest, DateTime.Today.AddDays(1), "Scheduled for tomorrow");
    private Task ClearDueDate(QuestDto quest) => UpdateQuestDate(quest, null, "Moved to inbox");

    private async Task PullNextQuestToToday()
    {
        if (NextQueuedQuest is not null)
        {
            await ScheduleToday(NextQueuedQuest);
        }
    }

    private async Task UpdateQuestDate(QuestDto quest, DateTime? dueDate, string message)
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            return;
        }

        var result = await QuestService.UpdateAsync(UserId, quest.Id, new QuestUpdateDto
        {
            DueDate = dueDate,
            ClearDueDate = dueDate is null
        });

        result.Match(
            mutation =>
            {
                ApplyMutation(quest.Id, mutation.Quest);
                ApplyMutationMeta(mutation);
                if (_expandedQuestId == quest.Id && _editDraft is not null)
                {
                    _editDraft.DueDate = dueDate;
                }
                Snackbar.Add(message, Severity.Success);
                return 0;
            },
            failed =>
            {
                Snackbar.Add(string.Join("; ", failed.errorMessage), Severity.Error);
                return 0;
            });
    }

    private async Task DeleteQuest(QuestDto quest)
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            return;
        }

        var result = await QuestService.DeleteAsync(UserId, quest.Id);
        result.Match(
            _ =>
            {
                _board.Quests.RemoveAll(q => q.Id == quest.Id);
                if (_expandedQuestId == quest.Id)
                {
                    CancelEdit();
                }
                Snackbar.Add("Quest deleted", Severity.Success);
                return 0;
            },
            failed =>
            {
                Snackbar.Add(string.Join("; ", failed.errorMessage), Severity.Error);
                return 0;
            });
    }

    private async Task OnSubtaskKeyDown(KeyboardEventArgs args, QuestDto quest)
    {
        if (args.Key == "Enter")
        {
            await AddSubtask(quest);
        }
    }

    private async Task AddSubtask(QuestDto quest)
    {
        var title = SubtaskDraft(quest.Id).Trim();
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(UserId))
        {
            return;
        }

        var result = await QuestService.AddSubtaskAsync(UserId, quest.Id, new QuestSubtaskCreateDto { Title = title });
        result.Match(
            mutation =>
            {
                ApplyMutation(quest.Id, mutation.Quest);
                ApplyMutationMeta(mutation);
                _subtaskDrafts[quest.Id] = string.Empty;
                return 0;
            },
            failed =>
            {
                Snackbar.Add(string.Join("; ", failed.errorMessage), Severity.Error);
                return 0;
            });
    }

    private async Task ToggleSubtask(QuestDto quest, QuestSubtaskDto subtask)
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            return;
        }

        var result = await QuestService.UpdateSubtaskAsync(UserId, quest.Id, subtask.Id, new QuestSubtaskUpdateDto
        {
            Completed = !subtask.Completed
        });
        result.Match(
            mutation =>
            {
                ApplyMutation(quest.Id, mutation.Quest);
                ApplyMutationMeta(mutation);
                return 0;
            },
            failed =>
            {
                Snackbar.Add(string.Join("; ", failed.errorMessage), Severity.Error);
                return 0;
            });
    }

    private async Task DeleteSubtask(QuestDto quest, QuestSubtaskDto subtask)
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            return;
        }

        var result = await QuestService.DeleteSubtaskAsync(UserId, quest.Id, subtask.Id);
        result.Match(
            _ =>
            {
                quest.Subtasks.RemoveAll(s => s.Id == subtask.Id);
                return 0;
            },
            failed =>
            {
                Snackbar.Add(string.Join("; ", failed.errorMessage), Severity.Error);
                return 0;
            });
    }

    private async Task SaveSkill()
    {
        var name = _newSkillName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        if (_editingSkillId is not null)
        {
            var skill = _board.Skills.FirstOrDefault(skill => skill.Id == _editingSkillId);
            if (skill is null)
            {
                CancelSkillEdit();
                return;
            }

            skill.Name = name;
            skill.Icon = _newSkillIcon;
            skill.Color = _newSkillColor;
            CancelSkillEdit();
            await Save("Skill updated");
            return;
        }

        _board.Skills.Add(new QuestSkillDto
        {
            Id = _temporaryId--,
            Name = name,
            Icon = _newSkillIcon,
            Color = _newSkillColor,
            Nodes =
            [
                new() { Id = _temporaryId--, Name = "First practice", SortOrder = 0 },
                new() { Id = _temporaryId--, Name = "Weekly streak", SortOrder = 1 },
                new() { Id = _temporaryId--, Name = "Personal project", SortOrder = 2 }
            ]
        });

        _newSkillName = string.Empty;
        await Save("Skill added");
    }

    private void EditSkill(QuestSkillDto skill)
    {
        _editingSkillId = skill.Id;
        _newSkillName = skill.Name;
        _newSkillIcon = ToFormIcon(skill.Icon);
        _newSkillColor = skill.Color;
    }

    private void CancelSkillEdit()
    {
        _editingSkillId = null;
        _newSkillName = string.Empty;
        _newSkillIcon = Icons.Material.Filled.Code;
        _newSkillColor = "#2563eb";
    }

    private async Task DeleteSkill(QuestSkillDto skill)
    {
        _board.Skills.Remove(skill);

        if (_editingSkillId == skill.Id)
        {
            CancelSkillEdit();
        }

        await Save("Skill deleted");
    }

    private async Task TrainSkill(QuestSkillDto skill)
    {
        skill.Xp += 40;
        _board.Xp += 15;
        await Save($"{skill.Name} training complete");
    }

    private async Task AddNode(QuestSkillDto skill)
    {
        var name = _newNodeName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        skill.Nodes.Add(new QuestSkillNodeDto
        {
            Id = _temporaryId--,
            Name = name,
            SortOrder = skill.Nodes.Count
        });
        _newNodeName = string.Empty;
        await Save("Node added");
    }

    private async Task UnlockNode(QuestSkillDto skill, QuestSkillNodeDto node)
    {
        if (node.Unlocked)
        {
            return;
        }

        node.Unlocked = true;
        node.UnlockedAt = DateTime.UtcNow;
        skill.Xp += 25;
        _board.Xp += 25;
        await Save($"{node.Name} unlocked");
    }

    private async Task Save(string message)
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            return;
        }

        _board = await QuestService.SaveSkillsAsync(UserId, _board);
        Snackbar.Add(message, Severity.Success);
    }

    private IEnumerable<QuestDto> FilterBaseQuests(QuestFilter filter)
    {
        return filter switch
        {
            QuestFilter.Today => _board.Quests.Where(quest => !quest.Completed && (IsToday(quest.DueDate) || IsOverdue(quest.DueDate))),
            QuestFilter.Upcoming => _board.Quests.Where(quest => !quest.Completed && quest.DueDate.HasValue && quest.DueDate.Value.Date > DateTime.Today),
            QuestFilter.Inbox => _board.Quests.Where(quest => !quest.Completed && quest.DueDate is null),
            _ => _board.Quests
        };
    }

    private IEnumerable<QuestDto> ApplySearchAndTag(IEnumerable<QuestDto> quests)
    {
        var query = quests;
        if (!string.IsNullOrWhiteSpace(_searchQuery))
        {
            var term = _searchQuery.Trim();
            query = query.Where(quest =>
                quest.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (quest.Notes?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                quest.Tags.Any(tag => tag.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(_tagFilter))
        {
            query = query.Where(quest => quest.Tags.Any(tag => string.Equals(tag, _tagFilter, StringComparison.OrdinalIgnoreCase)));
        }

        return query
            .OrderBy(quest => quest.Completed)
            .ThenBy(quest => quest.DueDate is null)
            .ThenBy(quest => quest.DueDate)
            .ThenByDescending(quest => quest.Priority)
            .ThenBy(quest => quest.SortOrder)
            .ThenBy(quest => quest.Id);
    }

    private List<QuestSection> BuildQuestSections()
    {
        var quests = VisibleQuests;
        List<QuestSection> sections = _filter switch
        {
            QuestFilter.Today =>
            [
                new("Overdue", "Needs a decision or a quick win.", Icons.Material.Filled.WarningAmber, "danger", quests.Where(q => IsOverdue(q.DueDate)).ToList()),
                new("Today", "The quests competing for attention now.", Icons.Material.Filled.Today, "accent", quests.Where(q => IsToday(q.DueDate)).ToList())
            ],
            QuestFilter.Upcoming =>
            [
                new("Next", "Scheduled quests coming up soon.", Icons.Material.Filled.Event, "accent", quests.Where(q => q.DueDate.HasValue && q.DueDate.Value.Date <= DateTime.Today.AddDays(7)).ToList()),
                new("Later", "Useful, but not for today.", Icons.Material.Filled.CalendarMonth, "muted", quests.Where(q => q.DueDate.HasValue && q.DueDate.Value.Date > DateTime.Today.AddDays(7)).ToList())
            ],
            QuestFilter.Inbox =>
            [
                new("Inbox", "Captured quests waiting for a date.", Icons.Material.Filled.LibraryBooks, "muted", quests.ToList())
            ],
            _ =>
            [
                new("Active", "Open quests across every lane.", Icons.Material.Filled.Bolt, "accent", quests.Where(q => !q.Completed).ToList()),
                new("Completed", "Finished quests and claimed rewards.", Icons.Material.Filled.CheckCircle, "success", quests.Where(q => q.Completed).ToList())
            ]
        };

        return sections.Where(section => section.Quests.Count > 0).ToList();
    }

    private int FilterCount(QuestFilter filter) => FilterBaseQuests(filter).Count();

    private void ToggleTag(string tag)
    {
        _tagFilter = _tagFilter == tag ? null : tag;
    }

    private string SubtaskDraft(int questId) => _subtaskDrafts.TryGetValue(questId, out var draft) ? draft : string.Empty;

    private void SetSubtaskDraft(int questId, string value) => _subtaskDrafts[questId] = value;

    private static int SubtaskCompletedCount(QuestDto quest) => quest.Subtasks.Count(subtask => subtask.Completed);

    private static int SubtaskProgress(QuestDto quest) =>
        quest.Subtasks.Count == 0 ? 0 : SubtaskCompletedCount(quest) * 100 / quest.Subtasks.Count;

    private void ApplyMutation(int questId, QuestDto quest)
    {
        var index = _board.Quests.FindIndex(q => q.Id == questId);
        if (index >= 0)
        {
            _board.Quests[index] = quest;
        }
        else
        {
            _board.Quests.Insert(0, quest);
        }
    }

    private void ApplyMutationMeta(QuestMutationResultDto mutation)
    {
        _board.Xp = mutation.TotalXp;
        _board.CurrentStreakDays = mutation.CurrentStreakDays;
        _board.LongestStreakDays = mutation.LongestStreakDays;

        if (mutation.AwardedSkillId is int skillId && mutation.AwardedSkillXp is int skillXp)
        {
            var skill = _board.Skills.FirstOrDefault(skill => skill.Id == skillId);
            if (skill is not null)
            {
                skill.Xp += skillXp;
            }
        }

        foreach (var achievement in mutation.UnlockedAchievements)
        {
            if (_board.Achievements.All(a => a.Code != achievement.Code))
            {
                _board.Achievements.Insert(0, achievement);
                Snackbar.Add($"Achievement unlocked: {achievement.Title}", Severity.Success);
            }
        }
    }

    private static string CompletionMessage(QuestMutationResultDto mutation)
    {
        var message = $"+{mutation.Quest.RewardXp} XP earned";
        if (mutation.AwardedSkillXp is int skillXp)
        {
            message += $" · +{skillXp} skill XP";
        }
        if (mutation.SpawnedQuest is not null)
        {
            message += " · next recurrence created";
        }
        return message;
    }

    private static List<string> ParseTags(string tags)
    {
        return tags
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
    }

    private static bool IsToday(DateTime? value) => IsSameDay(value, DateTime.Today);

    private static bool IsOverdue(DateTime? value) => value.HasValue && value.Value.Date < DateTime.Today;

    private static bool IsSameDay(DateTime? value, DateTime day) => value.HasValue && value.Value.Date == day.Date;

    private static string DueState(QuestDto quest)
    {
        if (quest.DueDate is null) return "inbox";
        if (IsOverdue(quest.DueDate)) return "overdue";
        if (IsToday(quest.DueDate)) return "today";
        return quest.DueDate.Value.Date <= DateTime.Today.AddDays(3) ? "soon" : "later";
    }

    private static string DueDateLabel(QuestDto quest)
    {
        if (quest.DueDate is null) return "Inbox";
        var date = quest.DueDate.Value.Date;
        var today = DateTime.Today;
        if (date == today) return "Today";
        if (date == today.AddDays(1)) return "Tomorrow";
        if (date == today.AddDays(-1)) return "Yesterday";
        var diff = (date - today).Days;
        if (diff < -1 && diff >= -7) return $"{Math.Abs(diff)}d overdue";
        if (diff > 1 && diff <= 7) return $"In {diff}d";
        return date.ToString("MMM d");
    }

    private static string TypeLabel(QuestType type) => type switch
    {
        QuestType.Main => "Main",
        QuestType.Faction => "Faction",
        _ => "Sub"
    };

    private static string PriorityLabel(QuestPriority priority) => priority switch
    {
        QuestPriority.High => "High",
        QuestPriority.Low => "Low",
        _ => "Medium"
    };

    private static string RecurrenceLabel(QuestRecurrence recurrence) => recurrence switch
    {
        QuestRecurrence.Daily => "Daily",
        QuestRecurrence.Weekly => "Weekly",
        QuestRecurrence.Monthly => "Monthly",
        _ => "No repeat"
    };

    private static string FilterLabel(QuestFilter filter) => filter switch
    {
        QuestFilter.Today => "Focus",
        QuestFilter.Upcoming => "Upcoming",
        QuestFilter.Inbox => "Inbox",
        _ => "All"
    };

    private static string FilterIcon(QuestFilter filter) => filter switch
    {
        QuestFilter.Today => Icons.Material.Filled.Today,
        QuestFilter.Upcoming => Icons.Material.Filled.Event,
        QuestFilter.Inbox => Icons.Material.Filled.LibraryBooks,
        _ => Icons.Material.Filled.FilterList
    };

    private static string TypeIcon(QuestType type) => type switch
    {
        QuestType.Main => Icons.Material.Filled.Map,
        QuestType.Faction => Icons.Material.Filled.Groups,
        _ => Icons.Material.Filled.Flag
    };

    private static int SkillLevel(QuestSkillDto skill)
    {
        return (skill.Xp / 100) + 1;
    }

    private static int SkillProgress(QuestSkillDto skill)
    {
        return skill.Xp % 100;
    }

    private static string ToMudIcon(string icon)
    {
        return icon switch
        {
            var value when value == Icons.Material.Filled.Brush => Icons.Material.Filled.Brush,
            var value when value == Icons.Material.Filled.Restaurant => Icons.Material.Filled.Restaurant,
            var value when value == Icons.Material.Filled.MenuBook => Icons.Material.Filled.MenuBook,
            var value when value == Icons.Material.Filled.Code => Icons.Material.Filled.Code,
            "brush-outline" => Icons.Material.Filled.Brush,
            "restaurant-outline" => Icons.Material.Filled.Restaurant,
            "book-outline" => Icons.Material.Filled.MenuBook,
            _ => Icons.Material.Filled.Code
        };
    }

    private static string ToFormIcon(string icon)
    {
        return ToMudIcon(icon);
    }

    private sealed record QuestSection(string Title, string Subtitle, string Icon, string Tone, List<QuestDto> Quests);

    private sealed class QuestEditDraft
    {
        public string Title { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public QuestType Type { get; set; }
        public QuestPriority Priority { get; set; }
        public QuestRecurrence Recurrence { get; set; }
        public DateTime? DueDate { get; set; }
        public string Tags { get; set; } = string.Empty;
        public int? MyGameId { get; set; }
        public int? SkillId { get; set; }

        public static QuestEditDraft From(QuestDto quest)
        {
            return new QuestEditDraft
            {
                Title = quest.Title,
                Notes = quest.Notes ?? string.Empty,
                Type = quest.Type,
                Priority = quest.Priority,
                Recurrence = quest.Recurrence,
                DueDate = quest.DueDate?.Date,
                Tags = string.Join(", ", quest.Tags),
                MyGameId = quest.MyGameId,
                SkillId = quest.SkillId
            };
        }
    }
}
