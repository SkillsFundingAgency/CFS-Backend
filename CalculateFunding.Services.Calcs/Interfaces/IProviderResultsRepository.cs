using CalculateFunding.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Search.Results;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IProviderResultsRepository
    {
        Task<IEnumerable<ProviderResult>> GetProviderResultsBySpecificationId(string specificationId);

        Task<HttpStatusCode> UpdateProviderResults(IEnumerable<ProviderResult> providerResults);

        Task<ProviderSearchResults> SearchProviders(SearchModel searchModel);
    }
}
