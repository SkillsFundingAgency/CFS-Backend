using CalculateFunding.Api.External.V4.Interfaces;

namespace CalculateFunding.Api.External.V4.Services
{
    public class ExternalApiFileSystemCacheSettings : IExternalApiFileSystemCacheSettings
    {
        public bool IsEnabled { get; set; } = true;
    }
}