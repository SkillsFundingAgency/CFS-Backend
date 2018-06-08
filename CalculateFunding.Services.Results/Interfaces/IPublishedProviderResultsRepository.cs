using CalculateFunding.Models.Results;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IPublishedProviderResultsRepository
    {
        Task CreatePublishedResults(IEnumerable<PublishedProviderResult> publishedResults);

        Task<IEnumerable<PublishedProviderResult>> GetPublishedProviderResultsForSpecificationId(string specificationId);
    }
}
