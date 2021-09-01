using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V4.Services
{
    public interface IChannelUrlToIdResolver
    {
        Task<int?> ResolveUrlToChannelId(string urlKey);
    }
}