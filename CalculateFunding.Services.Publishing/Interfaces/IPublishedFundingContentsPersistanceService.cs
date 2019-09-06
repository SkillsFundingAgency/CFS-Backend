using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedFundingContentsPersistanceService
    {
        Task SavePublishedFundingContents(IEnumerable<PublishedFundingVersion> publishedFundingToSave, TemplateMetadataContents templateMetadataContents);
    }
}
