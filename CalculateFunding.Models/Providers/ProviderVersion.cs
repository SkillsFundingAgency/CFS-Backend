using System.Collections.Generic;

namespace CalculateFunding.Models.Providers
{
    public class ProviderVersion : ProviderVersionMetadata
    {
        public IEnumerable<Provider> Providers { get; set; }
    }
}
