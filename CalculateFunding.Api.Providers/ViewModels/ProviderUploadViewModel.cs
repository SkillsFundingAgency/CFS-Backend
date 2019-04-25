using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Api.Providers.ViewModels
{
    public class ProviderUploadViewModel
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public IEnumerable<ProviderViewModel> Providers { get; set; }
    }
}
