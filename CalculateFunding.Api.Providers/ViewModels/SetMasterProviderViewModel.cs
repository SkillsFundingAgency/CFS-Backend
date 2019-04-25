using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Api.Providers.ViewModels
{
    public class SetMasterProviderViewModel
    {
        [Required]
        public string ProviderVersionId { get; set; }
    }
}
