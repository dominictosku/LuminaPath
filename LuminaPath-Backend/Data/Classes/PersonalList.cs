using Data.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Classes
{
    public enum ProgressionStatus
    {
        Completed,
        InProgress,
        Planned
    }
    public abstract class PersonalList : IBasicInfo
	{
        public int Id { get; set; }
		[Range(1, 10)]
		public byte? Rating { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ProgressionStatus Status { get; set; }
        public int? TimeSpend { get; set; }
    }
}
