using Domain.Models.Base;

namespace Domain.Common.Entities
{
    public class MediaFilter
    {
        public string? SearchString { get; set; }
        public ProgressionStatus Status { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? Publisher { get; set; }
        public bool MyMedia { get; set; }
        public Paging Paging { get; set; } = new Paging();
    }
}
