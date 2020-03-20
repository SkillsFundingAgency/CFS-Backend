using CalculateFunding.Models.Publishing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedFundingVersionDataService
    {
        Task<IEnumerable<PublishedFundingVersion>> GetPublishedFundingVersion(string fundingStreamId, string fundingPeriodId);
    }
}
