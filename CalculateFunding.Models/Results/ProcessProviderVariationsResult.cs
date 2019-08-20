using System;
using System.Collections.Generic;
using CalculateFunding.Models.Providers;

namespace CalculateFunding.Models.Results
{
    [Obsolete]
    public class ProcessProviderVariationsResult
    {
        public IEnumerable<ProviderVariationError> Errors { get; set; }

        public IEnumerable<ProviderChangeItem> ProviderChanges { get; set; }
    }
}
