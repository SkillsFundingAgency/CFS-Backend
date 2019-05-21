using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Models.Providers.ViewModels
{
    public class ProviderVersionViewModel : ProviderVersionHeaderViewModel
    {
        public IEnumerable<ProviderViewModel> Providers { get; set; }
    }
}
