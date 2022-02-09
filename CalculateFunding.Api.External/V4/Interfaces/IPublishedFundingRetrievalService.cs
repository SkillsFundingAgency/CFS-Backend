using CalculateFunding.Models.External.V4;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V4.Interfaces
{
    public interface IPublishedFundingRetrievalService
    {
        /// <summary>
        /// Gets a single published funding feed document.
        /// </summary>
        /// <param name="fundingId">Funding ID</param>
        /// <param name="channelCode">Channel Code</param>
        /// <param name="isForPreLoad"></param>
        /// <returns></returns>
        Task<Stream> GetFundingFeedDocument(string fundingId, string channelCode, bool isForPreLoad = false);

        Task<IDictionary<ExternalFeedFundingGroupItem, Stream>> GetFundingFeedDocuments(IEnumerable<ExternalFeedFundingGroupItem> batchItems, string channelCode, CancellationToken cancellationToken);
    }
}
