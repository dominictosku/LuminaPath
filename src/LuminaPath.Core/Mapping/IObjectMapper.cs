namespace LuminaPath.Core.Mapping
{
    public interface IObjectMapper
    {
        TDestination Map<TDestination>(object? source);
        TDestination Map<TSource, TDestination>(TSource source);
    }
}
