using AutoMapper;
using Data.Models;
using Data.Models.Base;
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

			CreateMap<Game, GamesDto>()
				.ForMember(dest => dest.PersonalGamings, act => act.MapFrom(src => src.PersonalGamings.FirstOrDefault()))
				.ReverseMap();

			CreateMap<GamesQuestDto, GamesQuest>()
				.ReverseMap();
		}
	}
}
