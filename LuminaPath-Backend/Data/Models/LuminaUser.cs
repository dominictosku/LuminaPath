using Data.Models.Gaming;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Models
{
	public class LuminaUser : IdentityUser
    {
        public string? RefreshToken { get; set; }
        public List<MyGame>? PersonalGamings { get; set; }
    }
}
