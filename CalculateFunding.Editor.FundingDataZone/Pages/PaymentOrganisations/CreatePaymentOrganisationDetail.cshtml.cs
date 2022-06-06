using System.Threading.Tasks;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using CalculateFunding.Services.FundingDataZone.SqlModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CalculateFunding.Editor.FundingDataZone.Pages.Provider
{
    public class CreatePaymentOrganisationDetailModel : PageModel
    {
        private readonly IPublishingAreaEditorRepository _repo;

        public CreatePaymentOrganisationDetailModel(IPublishingAreaEditorRepository publishingAreaEditorRepository)
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
                //await PopulateFundingStreams();
                return Page();
            }

            Organisation = await _repo.InsertOrganisation(Organisation);

            return RedirectToPage("/PaymentOrganisations/PaymentOrganisationDetail", new { providerSnapshotId = Organisation.ProviderSnapshotId, OrganisationId = Organisation.PaymentOrganisationId });
        }

        [BindProperty]
        public PublishingAreaOrganisation Organisation { get; set; }
    }
}
