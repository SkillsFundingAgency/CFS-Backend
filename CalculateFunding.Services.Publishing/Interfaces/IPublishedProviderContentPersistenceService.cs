using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderContentPersistenceService
    {
        Task SavePublishedProviderContents(TemplateMetadataContents templateMetadataContents, Common.ApiClient.Calcs.Models.TemplateMapping templateMapping, IEnumerable<PublishedProvider> publishedProvidersToUpdate, IPublishedProviderContentsGenerator generator, bool publishAll = false);
    }
}
