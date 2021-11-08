using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IPublishedFundingContentsChannelPersistanceService
    {
        Task SavePublishedFundingContents(
            IEnumerable<PublishedFundingVersion> publishedFundingToSave,
            Channel channel);
    }
}
