using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.IO;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V4.Services
{
    public interface IChannelUrlToChannelResolver
    {
        Task<Channel> ResolveUrlToChannel(string urlKey);

        Task<Stream> GetContentWithChannelVersion(Stream content, string channelCode);
        Task<Stream> GetContentWithChannelProviderVersion(Stream content, string channelCode);
    }
}