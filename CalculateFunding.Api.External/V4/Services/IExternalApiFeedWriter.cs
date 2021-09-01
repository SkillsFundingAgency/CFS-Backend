using CalculateFunding.Models.External;
using CalculateFunding.Models.External.V4;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V4.Services
{
    public interface IExternalApiFeedWriter
    {
        Task OutputFeedHeader(SearchFeedResult<ExternalFeedFundingGroupItem> searchFeed, string fundingUrl, PipeWriter writer);
        Task OutputFeedFooter(PipeWriter writer);
        Task OutputFeedItem(PipeWriter writer, string link, ExternalFeedFundingGroupItem feedItem, Stream contents, bool hasMoreItems);
    }
}