using System;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Api.Providers.ViewModels
{
    public class ProviderDateInformationViewModel
    {
        [Required]
        public string ProviderVersionId { get; set; }

        [Required]
        public DateTime Date { get; set; }
    }
}
