using System;
using CacheCow.Server;
using CalculateFunding.Models.Policy;

namespace CalculateFunding.Services.Policy.Caching.Http
{
    public class TemplateMatadataContentsTimedETagExtractor : ITimedETagExtractor<FundingStructure>
    {
        public TimedEntityTagHeaderValue Extract(FundingStructure viewModel) =>  new TimedEntityTagHeaderValue(viewModel.LastModified.ToETagString());     

        public TimedEntityTagHeaderValue Extract(object viewModel)
        {
            if (viewModel is FundingStructure metadataContents)
            {
                return Extract(metadataContents);
            }
             
            throw new ArgumentOutOfRangeException(nameof(viewModel),
                $"Can only extract timed etag for TemplateMetadataContents but was supplied {viewModel?.GetType().Name ?? "nothing"}");
        }
    }
}