using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishPrerequisiteChecker
    {
        Task<IEnumerable<string>> PerformPrerequisiteChecks(SpecificationSummary specification, IEnumerable<PublishedProvider> publishedProviders);
    }
}
