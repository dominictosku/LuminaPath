namespace LuminaPath.Core.Entities
{
    public class Paging
    {
        public const int DefaultCount = 10;
        public const int MaxCount = 100;

        private int _pageIndex = 1;
        private int _count;

        public Paging() { }
        public Paging(int pageIndex, int count)
        {
            PageIndex = pageIndex;
            Count = count;
        }

        public int PageIndex
        {
            get => _pageIndex;
            set => _pageIndex = Math.Max(1, value);
        }

        public int Count
        {
            get => _count;
            set => _count = value <= 0 ? 0 : Math.Min(value, MaxCount);
        }

        public int EffectiveCount => Count > 0 ? Count : DefaultCount;
    }
}
