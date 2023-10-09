using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Models.Dto.Blob
{
    public class BlobResponseDto
    {
        public string? Status { get; set; }
        public bool Error { get; set; }
        public BlobDto Blob { get; set; }

        public BlobResponseDto()
        {
            Blob = new BlobDto();
        }
    }
}
