using System;
using System.Collections.Generic;

namespace CalculateFunding.Api.Providers.ViewModels
{
    public class MasterProviderDatasetResultViewModel
    {
        public string ProviderVersionId { get; set; }

        public IEnumerable<MasterProviderViewModel> Providers { get; set; }

        public DateTimeOffset Created { get; set; }
    }
}
