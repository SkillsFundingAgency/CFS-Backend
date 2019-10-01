namespace CalculateFunding.Services.Providers
{
    public class ProviderVersionServiceSettings : IProviderVersionServiceSettings
    {
        public bool IsFileSystemCacheEnabled { get; set; } = true;
    }
}