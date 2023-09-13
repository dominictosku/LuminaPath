using AutoMapper;
using Data.Models;
using Data.Models.Dto;
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

			CreateMap<GamesDto, Game>()
				.ReverseMap();

			CreateMap<GamesQuestDto, GamesQuest>()
				.ReverseMap();
		}
	}
}
