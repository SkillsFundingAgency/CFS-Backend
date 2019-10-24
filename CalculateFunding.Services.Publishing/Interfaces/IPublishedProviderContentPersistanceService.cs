﻿using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderContentPersistanceService
    {
        Task SavePublishedProviderContents(TemplateMetadataContents templateMetadataContents, Common.ApiClient.Calcs.Models.TemplateMapping templateMapping, Dictionary<string, GeneratedProviderResult> generatedPublishedProviderData, IEnumerable<PublishedProvider> publishedProvidersToUpdate, IPublishedProviderContentsGenerator generator);
    }
}