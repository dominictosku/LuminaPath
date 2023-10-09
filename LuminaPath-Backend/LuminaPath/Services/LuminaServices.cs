using Data.Interfaces;
using Data.Models.Gaming;
using Data.Models.Quests;
using Data.Repositories;
using Data.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminaPath.Services
{
    public static class LuminaServices
    {
        public static void AddLuminaServices(this WebApplicationBuilder builder)
        {
            string connectionString = Environment.GetEnvironmentVariable("AZURE_CONNECTIONSTRING")
                ?? builder.Configuration.GetSection("Azure")["BlobConnectionString"]
                ?? throw new Exception("No blob connectionfound");
            string containerName = builder.Configuration.GetSection("Azure")["BlobContainerName"]
                ?? throw new Exception("No blob container name found");

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddScoped<ITokenGenerator, JwtService>();
            builder.Services.AddTransient<IGenericRepo<Game>, GenericRepo<Game>>();
            builder.Services.AddTransient<IGenericRepo<MyGame>, GenericRepo<MyGame>>();
            builder.Services.AddTransient<IGenericRepo<GamesQuest>, GenericRepo<GamesQuest>>();
            builder.Services.AddScoped<IAzureStorage, AzureStorage>(s =>
                new AzureStorage(connectionString, containerName, s.GetRequiredService<ILogger<AzureStorage>>()));
            builder.Services.AddTransient<GameRepo>();
            builder.Services.AddTransient<MyGameRepo>();
            builder.Services.AddAutoMapper(typeof(Program));
        }
    }
}
