using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Providers.ViewModels;
using CalculateFunding.Repositories.Common.Search;

namespace CalculateFunding.Api.Providers.ViewModels
{

    public class ProviderVersionSearchResults
    {
        public ProviderVersionSearchResults()
        {
            Results = Enumerable.Empty<ProviderVersionViewModel>();
            Facets = Enumerable.Empty<Facet>();
        }

        public int TotalCount { get; set; }

        public IEnumerable<ProviderVersionViewModel> Results { get; set; }

        public IEnumerable<Facet> Facets { get; set; }
    }
}
