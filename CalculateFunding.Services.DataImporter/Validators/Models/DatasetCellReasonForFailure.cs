namespace CalculateFunding.Services.DataImporter.Validators.Models
{
    public enum DatasetCellReasonForFailure
    {
        None,
        DataTypeMismatch,
        MaxOrMinValueExceeded,
        ProviderIdValueMissing,
        DuplicateEntriesInTheProviderIdColumn,
        ProviderIdMismatchWithServiceProvider,
        NewProviderMissingAllDataSchemaFields,
        ExtraHeaderField,
        ProviderIdNotInCorrectFormat,
        DuplicateColumnHeader
    }
}