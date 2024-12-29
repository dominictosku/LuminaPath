namespace LuminaPath.Core.Common.Interfaces
{
    public interface IMedia<TDocument> : IBasicInfo where TDocument : IDocument
    {
        public TDocument? Image { get; set; }
    }
}
