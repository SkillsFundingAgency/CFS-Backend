using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Providers.ViewModels;
using CalculateFunding.Repositories.Common.Search;

namespace CalculateFunding.Api.Providers.ViewModels
{

    public class ProviderSearchResults
    {
        public ProviderSearchResults()
        {
            Results = Enumerable.Empty<ProviderViewModel>();
            Facets = Enumerable.Empty<Facet>();
        }

        public int TotalCount { get; set; }

        public IEnumerable<ProviderViewModel> Results { get; set; }

        public IEnumerable<Facet> Facets { get; set; }
    }
}
