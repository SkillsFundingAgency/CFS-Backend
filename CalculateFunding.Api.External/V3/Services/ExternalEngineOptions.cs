using CalculateFunding.Api.External.V3.Interfaces;

namespace CalculateFunding.Api.External.V3.Services
{
    public class ExternalEngineOptions : IExternalEngineOptions
    {
        public int BlobLookupConcurrencyCount { get; set; }
    }
}
