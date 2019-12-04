namespace CalculateFunding.Services.Providers
{
    public class ScopedProvidersServiceSettings : IScopedProvidersServiceSettings
    {
        public bool IsFileSystemCacheEnabled { get; set; } = true;
    }
}