using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Generators.Schema10
{
    public class PublishedFundingIdGenerator : IPublishedFundingIdGenerator
    {
        public string GetFundingId(PublishedFundingVersion publishedFunding)
        {
            return $"{publishedFunding.OrganisationGroupTypeIdentifier}_{publishedFunding.OrganisationGroupIdentifierValue}_{publishedFunding.FundingPeriod.Id}_{publishedFunding.FundingStreamId}_{publishedFunding.Version}";
        }
    }
}
