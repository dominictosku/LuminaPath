namespace LuminaPath.Core.Interfaces
{
    public interface IMedia<TDocument> : IBasicInfo where TDocument : IDocument
    {
        public string Name { get; set; }
        public TDocument? Image { get; set; }
    }
}
