using System.ComponentModel.DataAnnotations;

namespace LuminaPath.Core.Enums
{
    [Flags]
    public enum Plattforms
    {
        [Display(Name = "Playstation 4")]
        Playstation4 = 1,

        [Display(Name = "Playstation 5")]
        Playstation5 = 2,

        [Display(Name = "Switch")]
        Switch = 4,

        [Display(Name = "PC")]
        PC = 8,

        [Display(Name = "XBOX")]
        XBOX = 16
    }
}
