using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Providers.Interfaces;
using Newtonsoft.Json;

namespace CalculateFunding.DebugAllocationModel
{
    public class FilesystemCacheProviderService : IProviderService
    {
        private readonly IProviderService _providerService;

        public FilesystemCacheProviderService(IProviderService providerService)
        {
            _providerService = providerService;
        }

        public async Task<IEnumerable<ProviderSummary>> FetchCoreProviderData()
        {
            string cacheFilename = Path.Combine(Directory.GetCurrentDirectory(), "providers.json");

            if (File.Exists(cacheFilename))
            {
                string text = await File.ReadAllTextAsync(cacheFilename);

                if (!string.IsNullOrWhiteSpace(text))
                {
                    return JsonConvert.DeserializeObject<IEnumerable<ProviderSummary>>(text);
                }
            }

            IEnumerable<ProviderSummary> providers = await _providerService.FetchCoreProviderData();

            if (providers != null)
            {
                string cacheContent = JsonConvert.SerializeObject(providers);
                File.WriteAllText(cacheFilename, cacheContent);
            }

            return providers;
        }
    }
}
