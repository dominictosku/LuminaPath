using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Core.Interfaces
{
    public interface ILuminaPathDbContext
    {
        DbSet<Game> Games { get; set; }
        DbSet<MyGame> MyGames { get; set; }
        DbSet<Quest> Quests { get; set; }
        DbSet<QuestProfile> QuestProfiles { get; set; }
        DbSet<QuestSkill> QuestSkills { get; set; }
        DbSet<QuestSkillNode> QuestSkillNodes { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
