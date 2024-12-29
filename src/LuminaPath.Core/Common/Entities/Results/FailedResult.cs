namespace LuminaPath.Core.Common.Entities.Results
{
    public record FailedResult(IEnumerable<string> errorMessage)
    {
        public FailedResult(string error) : this(new[] { error })
        {

        }
    }
}
