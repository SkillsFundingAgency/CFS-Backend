using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedFundingContentsPersistanceService : IPublishedFundingContentsPersistanceService
    {
        private readonly IPublishedFundingContentsGeneratorResolver _publishedFundingContentsGeneratorResolver;

        public Task SavePublishedFundingContents(IEnumerable<PublishedFundingVersion> publishedFundingToSave, TemplateMetadataContents templateMetadataContents)
        {
            IPublishedFundingContentsGenerator generator = _publishedFundingContentsGeneratorResolver.GetService(templateMetadataContents.SchemaVersion);


            foreach (var publishedFunding in publishedFundingToSave)
            {
                string contents = generator.GenerateContents(publishedFunding, templateMetadataContents);

                if (string.IsNullOrWhiteSpace(contents))
                {
                    throw new RetriableException($"Generator failed to generate content for published provider version with id: '{publishedFunding.Id}'");
                }

                // Save to BLOB

                //  Save to Search
            }

            throw new NotImplementedException();
        }
    }
}
