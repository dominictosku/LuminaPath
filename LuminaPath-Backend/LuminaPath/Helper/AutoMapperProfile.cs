using AutoMapper;
using Data.Models;
using Data.Models.Dto;
using Org.BouncyCastle.Asn1.X509;

namespace LuminaPath.Helper
{
	public class AutoMapperProfile : Profile
	{
		public AutoMapperProfile() {
			CreateMap<PersonalGamingDto, PersonalGaming>()
				.ReverseMap();

			CreateMap<GamesDto, Games>()
				.ReverseMap();
		}
	}
}
