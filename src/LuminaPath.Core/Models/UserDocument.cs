using LuminaPath.Core.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminaPath.Core.Models
{
    public class UserDocument : Document
    {
        public string Album { get; set; } = string.Empty;
        public LuminaUser User { get; set; }
        public string UserId { get; set; }
    }
}
