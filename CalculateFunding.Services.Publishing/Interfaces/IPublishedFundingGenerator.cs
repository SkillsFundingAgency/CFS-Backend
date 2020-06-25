using System.Collections.Generic;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedFundingGenerator
    {
        IEnumerable<(PublishedFunding, PublishedFundingVersion)> GeneratePublishedFunding(
            PublishedFundingInput generatePublishedFundingInput, 
            IEnumerable<PublishedProvider> publishedProviders);
    }
}
