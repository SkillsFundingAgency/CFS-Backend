using CalculateFunding.Models.Providers.ViewModels;
using System;
using System.Collections.Generic;

namespace CalculateFunding.Api.Providers.ViewModels
{
    public class ProviderDatasetResultViewModel
    {
        public string ProviderVersionId { get; set; }

        public DateTimeOffset Created { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }


        public IEnumerable<ProviderViewModel> Providers { get; set; }

    }
}
