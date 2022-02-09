using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V4.Services
{
    public interface IChannelUrlToChannelResolver
    {
        Task<Channel> ResolveUrlToChannel(string urlKey);
    }
}