using LuminaPath.MauiClientApp.Database;
using LuminaPath.Core.Common.Interfaces;
using Microsoft.Extensions.Logging;
using LuminaPath.UI.Shared;
using LuminaPath.MauiClientApp.Services;
using LuminaPath.MauiClientApp.Profile;
using AutoMapper;

namespace LuminaPath.MauiClientApp
{
	public static class MauiProgram
	{
		public static MauiApp CreateMauiApp()
		{
			var builder = MauiApp.CreateBuilder();
			builder
				.UseMauiApp<App>()
				.ConfigureFonts(fonts =>
				{
					fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				});

			builder.Services.AddMauiBlazorWebView();
			builder.Services.AddUILibrary();
			builder.Services.AddSingleton<GamesDatabase>();
			builder.Services.AddSingleton<IStorageService, FileService>();
			builder.Services.AddAutoMapper(typeof(LuminaMapperProfile));

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

			return builder.Build();
		}
	}
}
