using LuminaPath.Core.Enums;
using LuminaPath.Core.Extensions;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models;

namespace LuminaPath.Features.Media.Games.ViewModel
{
    public class GameViewModel : Game, IMedia<MediaDocument>
    {
        public string? PsnId
        {
            get => ExternalIds.GetExternalId(ExternalMediaProvider.Psn);
            set => ExternalIds.SetExternalId(ExternalMediaProvider.Psn, value);
        }

        public string? SteamId
        {
            get => ExternalIds.GetExternalId(ExternalMediaProvider.Steam);
            set => ExternalIds.SetExternalId(ExternalMediaProvider.Steam, value);
        }
    }
}
