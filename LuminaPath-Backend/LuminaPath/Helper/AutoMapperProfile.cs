using AutoMapper;
using Data.Models.Base;
using Data.Models.Dto.Gaming;
using Data.Models.Dto.Quests;
using Data.Models.Gaming;
using Data.Models.Quests;

namespace LuminaPath.Helper
{
	public class AutoMapperProfile : Profile
	{
		public AutoMapperProfile() {
			CreateMap<MyGameDto, MyGame>()
				.ReverseMap();

			CreateMap<GamesNoIncludeDto, Game>()
				.ReverseMap();

			CreateMap<Game, GamesDto>()
				.ForMember(dest => dest.MyGames, act => act.MapFrom(src => src.MyGames.FirstOrDefault()))
				.ReverseMap();

			CreateMap<GamesQuestDto, GamesQuest>()
				.ReverseMap();
		}
	}
}
