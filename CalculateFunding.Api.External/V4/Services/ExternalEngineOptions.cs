using CalculateFunding.Api.External.V4.Interfaces;

namespace CalculateFunding.Api.External.V4.Services
{
    public class ExternalEngineOptions : IExternalEngineOptions
    {
        public int BlobLookupConcurrencyCount { get; set; }
    }
}
