using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Core.Interfaces
{
    public interface ILuminaPathDbContext
    {
        DbSet<Game> Games { get; set; }
        DbSet<MyGame> MyGames { get; set; }
        DbSet<Anime> Animes { get; set; }
        DbSet<MyAnime> MyAnimes { get; set; }
        DbSet<Movie> Movies { get; set; }
        DbSet<MyMovie> MyMovies { get; set; }
        DbSet<Series> Series { get; set; }
        DbSet<MySeries> MySeries { get; set; }
        DbSet<Quest> Quests { get; set; }
        DbSet<QuestProfile> QuestProfiles { get; set; }
        DbSet<QuestSkill> QuestSkills { get; set; }
        DbSet<QuestSkillNode> QuestSkillNodes { get; set; }
        DbSet<ApplicationSetting> ApplicationSettings { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
