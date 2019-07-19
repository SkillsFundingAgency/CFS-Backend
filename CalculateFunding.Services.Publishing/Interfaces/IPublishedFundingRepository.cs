using CalculateFunding.Models.Publishing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedFundingRepository
    {
        Task<PublishedProviderVersion> GetPublishedProviderVersion(string fundingStreamId,
                string fundingPeriodId,
                string providerId,
                string version);
    }
}
