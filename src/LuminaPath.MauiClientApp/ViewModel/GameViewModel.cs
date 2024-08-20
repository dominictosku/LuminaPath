using LuminaPath.MauiClientApp.Models;
using System;
using System.Collections.Generic;
using LuminaPath.Core.Common.Interfaces;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminaPath.MauiClientApp.ViewModel
{
    public class GameViewModel : LocalGame, IMedia<LocalDocument>
    {
        public GameViewModel()
        {
        }

        public GameViewModel(LocalGame game) 
        {
            Id = game.Id;
            Name = game.Name;
            Description = game.Description;
            Plattforms = game.Plattforms;
        }

        public LocalMyGame? MyGame { get; set; }
        public LocalDocument? Image { get; set; }
    }
}
