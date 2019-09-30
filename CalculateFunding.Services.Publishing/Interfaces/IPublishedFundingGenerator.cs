using System.Collections.Generic;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedFundingGenerator
    {
        IEnumerable<(PublishedFunding, PublishedFundingVersion)> GeneratePublishedFunding(GeneratePublishedFundingInput generatePublishedFundingInput);
    }
}
