namespace CalculateFunding.Api.External.V3.Models
{
    public interface IExternalEngineOptions
    {
        int BlobLookupConcurrencyCount { get; }
    }
}