namespace CalculateFunding.Api.External.V3.Interfaces
{
    public interface IExternalEngineOptions
    {
        int BlobLookupConcurrencyCount { get; }
    }
}