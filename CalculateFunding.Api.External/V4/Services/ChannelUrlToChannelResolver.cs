using CalculateFunding.Services.Publishing.FundingManagement;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V4.Services
{
    public class ChannelUrlToChannelResolver : IChannelUrlToChannelResolver
    {
        private readonly IReleaseManagementRepository _repo;

        private Dictionary<string, Channel?> _keyCache = new Dictionary<string, Channel?>();

        public ChannelUrlToChannelResolver(IReleaseManagementRepository releaseManagementRepository)
        {
            _repo = releaseManagementRepository;
        }

        public async Task<Channel> ResolveUrlToChannel(string urlKey)
        {
            string normalisedKey = urlKey.ToLowerInvariant();

            if (_keyCache.TryGetValue(normalisedKey, out Channel result))
            {
                return result;
            }

            result = await _repo.GetChannelFromUrlKey(normalisedKey);

            _keyCache.Add(normalisedKey, result);

            return result;
        }
    }
}
