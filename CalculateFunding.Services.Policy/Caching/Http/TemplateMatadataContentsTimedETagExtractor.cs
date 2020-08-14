using System;
using CacheCow.Server;
using CalculateFunding.Common.TemplateMetadata.Models;

namespace CalculateFunding.Services.Policy.Caching.Http
{
    public class TemplateMatadataContentsTimedETagExtractor : ITimedETagExtractor<TemplateMetadataContents>
    {
        public TimedEntityTagHeaderValue Extract(TemplateMetadataContents viewModel) =>  new TimedEntityTagHeaderValue(viewModel.LastModified.ToETagString());     

        public TimedEntityTagHeaderValue Extract(object viewModel)
        {
            if (viewModel is TemplateMetadataContents metadataContents)
            {
                return Extract(metadataContents);
            }
             
            throw new ArgumentOutOfRangeException(nameof(viewModel),
                $"Can only extract timed etag for TemplateMetadataContents but was supplied {(viewModel?.GetType().Name ?? "nothing")}");
        }
    }
}