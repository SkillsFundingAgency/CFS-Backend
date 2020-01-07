using System.Collections.Generic;

namespace CalculateFunding.Models.Providers.ViewModels
{
    public class ProviderVersionViewModel : ProviderVersionMetadata
    {
        public IEnumerable<Provider> Providers { get; set; }
    }
}
