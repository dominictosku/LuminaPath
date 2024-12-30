using LuminaPath.Core.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminaPath.Core.Models
{
    public class MediaDocument : Document
    {
        public Media? Media { get; set; }
        public int? MediaId { get; set; }

    }
}
