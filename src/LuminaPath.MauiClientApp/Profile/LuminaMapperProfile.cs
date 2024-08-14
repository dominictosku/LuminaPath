using LuminaPath.MauiClientApp.Models;
using LuminaPath.MauiClientApp.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminaPath.MauiClientApp.Profile
{
	public class LuminaMapperProfile : AutoMapper.Profile
	{
		public LuminaMapperProfile()
		{
			CreateMap<GameViewModel, LocalGame>()
				.ReverseMap();
		}
	}
}
