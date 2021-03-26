namespace CalculateFunding.Models.Datasets.Converter
{
    public enum RowCopyOutcome
    {
        Unknown,
        Copied,
        ValidationFailure,
        DestinationRowAlreadyExists,
        SourceRowNotFound,
    }
}
