using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class ChannelsService : IChannelsService
    {
        private readonly IReleaseManagementRepository _repo;

        public ChannelsService(IReleaseManagementRepository releaseManagementRepository)
        {
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));

            _repo = releaseManagementRepository;
        }

        public async Task<IEnumerable<KeyValuePair<string, Channel>>> GetAndVerifyChannels(IEnumerable<string> channelCodes)
        {
            Dictionary<string, Channel> allChannels = (await _repo.GetChannels()).ToDictionary(_ => _.ChannelCode);

            Dictionary<string, Channel> channels = new Dictionary<string, Channel>(channelCodes.Count());

            foreach (string channelCode in channelCodes)
            {
                if (!allChannels.ContainsKey(channelCode))
                {
                    throw new InvalidOperationException($"Channel with code '{channelCode}' does not exist");
                }

                channels.Add(channelCode, allChannels[channelCode]);
            }

            return channels;
        }
    }
}
