namespace CalculateFunding.Api.External.V4.Interfaces
{
    public interface IExternalEngineOptions
    {
        int BlobLookupConcurrencyCount { get; }
    }
}