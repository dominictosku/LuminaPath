using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class MediaFile
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Uri { get; set; }
        public string? ContentType { get; set; }
    }
}
