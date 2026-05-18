namespace LuminaPath.Core.Dtos
{
    public class UserGridPageDto
    {
        public IReadOnlyList<UserGridItemDto> Items { get; set; } = [];
        public int Total { get; set; }
        public int Active { get; set; }
        public int Locked { get; set; }
    }
}
