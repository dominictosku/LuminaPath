using LuminaPath.MauiClientApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminaPath.MauiClientApp.ViewModel
{
    public class GameViewModel : LocalGame
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
        public LocalDocument? Image { get; set; }
    }
}
