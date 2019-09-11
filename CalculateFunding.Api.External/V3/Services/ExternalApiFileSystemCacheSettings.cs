using CalculateFunding.Api.External.V3.Interfaces;

namespace CalculateFunding.Api.External.V3.Services
{
    public class ExternalApiFileSystemCacheSettings : IExternalApiFileSystemCacheSettings
    {
        public bool IsEnabled { get; set; } = true;
    }
}