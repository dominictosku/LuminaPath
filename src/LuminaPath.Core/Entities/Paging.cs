namespace LuminaPath.Core.Entities
{
    public class Paging
    {
        public Paging() { }
        public Paging(int pageIndex, int count)
        {
            PageIndex = pageIndex;
            Count = count;
        }

        public int PageIndex { get; set; } = 1;
        public int Count { get; set; }
    }
}
