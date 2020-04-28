using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPrerequisiteChecker
    {
        Task PerformChecks<TSpecification>(
            TSpecification prereqObject, 
            string jobId, 
            IEnumerable<PublishedProvider> publishedProviders = null, 
            IEnumerable<string> providerIds = null);

        bool IsCheckerType(PrerequisiteCheckerType type);
    }
}
