namespace LuminaPath.Core.Entities.Results
{
    public record ValidationFailed : FailedResult
    {
        public IReadOnlyList<ValidationFailure> Errors { get; }

        public ValidationFailed(IEnumerable<ValidationFailure> errors)
            : base(errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"))
        {
            Errors = errors.ToList();
        }

        public ValidationFailed(ValidationFailure error) : this(new[] { error })
        {
        }
    }
}
