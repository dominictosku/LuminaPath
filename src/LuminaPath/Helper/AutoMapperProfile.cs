using Application.Common.Features.Gaming.Dto;
using Application.Common.Features.Quests.Dto;
using AutoMapper;
using Domain.Models;
using LuminaPath.ViewModel;

namespace LuminaPath.Helper
{
    public class AutoMapperProfile : Profile
	{
		public AutoMapperProfile()
		{
			CreateMap<MyGameDto, MyGame>()
				.ReverseMap();

			CreateMap<GamesNoIncludeDto, Game>()
				.ReverseMap();

			CreateMap<Game, GamesDto>()
				.ForMember(dest => dest.MyGames, act => act.MapFrom(src => src.MyGames.FirstOrDefault()))
				.ReverseMap();

			CreateMap<Game, GameViewModel>()
				.ReverseMap();

			CreateMap<GamesQuestDto, GamesQuest>()
				.ReverseMap();
		}
	}
}
