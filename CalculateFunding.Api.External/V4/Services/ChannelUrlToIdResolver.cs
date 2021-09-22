using CalculateFunding.Services.Publishing.FundingManagement;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V4.Services
{
    public class ChannelUrlToIdResolver : IChannelUrlToIdResolver
    {
        private readonly IReleaseManagementRepository _repo;

        private Dictionary<string, int?> _keyCache = new Dictionary<string, int?>();

        public ChannelUrlToIdResolver(IReleaseManagementRepository releaseManagementRepository)
        {
            _repo = releaseManagementRepository;
        }

        public async Task<int?> ResolveUrlToChannelId(string urlKey)
        {
            string normalisedKey = urlKey.ToLowerInvariant();

            if (_keyCache.TryGetValue(normalisedKey, out int? result))
            {
                return result;
            }

            result = await _repo.GetChannelIdFromUrlKey(normalisedKey);

            _keyCache.Add(normalisedKey, result);

            return result;
        }
    }
}
