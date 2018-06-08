using CalculateFunding.Models;
using CalculateFunding.Models.Results;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IPublishedProviderResultsAssemblerService
    {
        Task<IEnumerable<PublishedProviderResult>> Assemble(IEnumerable<ProviderResult> providerResults, Reference author, string specificationId);
    }
}
