using Core.Models.Gaming;
using Microsoft.AspNetCore.Components.Forms;

namespace LuminaPath.ViewModel
{
    public class GameViewModel : Game
    {
        public Stream? File { get; set; }
    }
}
