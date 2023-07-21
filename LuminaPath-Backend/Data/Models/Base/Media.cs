using Data.Interfaces;
using Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Models.Base
{
    [Index(nameof(Name), IsUnique = true)]
    public abstract class Media : IBasicInfo
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Title")]
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? Genre { get; set; }

		[Display(Name = "Release Date")]
		public DateTime? ReleaseDate { get; set; }
    }
}
