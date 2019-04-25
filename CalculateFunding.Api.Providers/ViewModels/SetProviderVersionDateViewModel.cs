using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Api.Providers.ViewModels
{
    public class SetProviderVersionDateViewModel
    {
        [Required]
        public string ProviderVersionId { get; set; }
    }
}
