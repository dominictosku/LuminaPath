using System.ComponentModel.DataAnnotations;

namespace LuminaPath.Core.Common.Enums
{
    [Flags]
    public enum Plattforms
    {
        [Display(Name = "Playstation")]
        Playstation = 1,
        [Display(Name = "Switch")]
        Switch = 2,
        [Display(Name = "PC")]
        PC = 4,
        [Display(Name = "XBOX")]
        XBOX = 8
    }
}
