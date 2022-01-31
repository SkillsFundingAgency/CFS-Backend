using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IPublishedFundingContentsChannelPersistenceService
    {
        Task SavePublishedFundingContents(
            IEnumerable<PublishedFundingVersion> publishedFundingToSave,
            Channel channel);
    }
}
