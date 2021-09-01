using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public interface IChannelsService
    {
        Task<IEnumerable<KeyValuePair<string, Channel>>> GetAndVerifyChannels(IEnumerable<string> channelCodes);
    }
}