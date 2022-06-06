using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CalculateFunding.Editor.FundingDataZone.Pages.ProviderStatusOptions
{
    public class CreateProviderStatusModel : PageModel
    {
        private readonly IPublishingAreaEditorRepository _repo;

        public CreateProviderStatusModel(IPublishingAreaEditorRepository publishingAreaEditorRepository)
        {
            _repo = publishingAreaEditorRepository;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            CalculateFunding.Services.FundingDataZone.SqlModels.ProviderStatus providerStatus = new CalculateFunding.Services.FundingDataZone.SqlModels.ProviderStatus()
            {
                ProviderStatusName = Name,
                ProviderStatusId = Id,
            };

            await _repo.CreateProviderStatus(providerStatus);

            return RedirectToPage("/ProviderStatusOptions/Index");
        }

        [BindProperty]
        [Required, StringLength(128)]
        [Display(Name = "Provider status name")]
        public string Name { get; set; }

        [BindProperty]
        [Required, Range(1, int.MaxValue)]
        [Display(Name = "Provider status id")]
        public int Id { get; set; }
    }
}
