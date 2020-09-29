using CacheCow.Server;
using CalculateFunding.Models.Publishing;
using System;
using System.Text;

namespace CalculateFunding.Services.Publishing.Caching.Http
{
    public class PublishedProviderFundingStructureTimedEtagExtractor : ITimedETagExtractor<PublishedProviderFundingStructure>
    {
        public TimedEntityTagHeaderValue Extract(PublishedProviderFundingStructure viewModel)
        {
            return new TimedEntityTagHeaderValue(viewModel.PublishedProviderVersion.ToString());
        }

        public TimedEntityTagHeaderValue Extract(object viewModel)
        {
            if (viewModel is PublishedProviderFundingStructure publishedProviderFundingStructure)
            {
                return Extract(publishedProviderFundingStructure);
            }

            throw new ArgumentOutOfRangeException(nameof(viewModel),
                $"Can only extract timed etag for PublishedProviderFundingStructure, but was supplied {viewModel?.GetType().Name ?? "nothing"}");
        }
    }
}
