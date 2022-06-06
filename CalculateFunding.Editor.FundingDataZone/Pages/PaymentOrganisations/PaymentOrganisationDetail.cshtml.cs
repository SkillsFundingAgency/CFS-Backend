using System;
using System.Threading.Tasks;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using CalculateFunding.Services.FundingDataZone.SqlModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CalculateFunding.Editor.FundingDataZone.Pages.Provider
{
    public class PaymentOrganisationDetailModel : PageModel
    {
        private readonly IPublishingAreaEditorRepository _repo;

        public PaymentOrganisationDetailModel(IPublishingAreaEditorRepository publishingAreaEditorRepository)
        {
            _repo = publishingAreaEditorRepository;
        }

        public async Task<IActionResult> OnGet([FromRoute] int providerSnapshotId, [FromRoute] string organisationId)
        {
            Organisation = await _repo.GetOrganisationInSnapshot(providerSnapshotId, organisationId);

            ProviderSnapshotId = providerSnapshotId.ToString();
            PaymentOrganisationId = Organisation.PaymentOrganisationId;

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            if (!ModelState.IsValid)
            {
                //await PopulateFundingStreams();
                return Page();
            }

            PublishingAreaOrganisation organisation = await _repo.GetOrganisationInSnapshot(Convert.ToInt32(ProviderSnapshotId), PaymentOrganisationId.ToString());

            organisation.Name = Organisation.Name;
            organisation.Ukprn = Organisation.Ukprn;
            organisation.PaymentOrganisationType = Organisation.PaymentOrganisationType;
            organisation.TrustCode = Organisation.TrustCode;
            organisation.Upin = Organisation.Upin;
            organisation.Urn = Organisation.Urn;
            organisation.CompanyHouseNumber = Organisation.CompanyHouseNumber;
            organisation.LaCode = Organisation.LaCode;

            if (await _repo.UpdateOrganisation(organisation))
            {
                return RedirectToPage("/PaymentOrganisations/PaymentOrganisationDetail", new { providerSnapshotId = organisation.ProviderSnapshotId, OrganisationId = organisation.PaymentOrganisationId });
            }

            return Page();
        }

        [BindProperty]
        public PublishingAreaOrganisation Organisation { get; set; }

        [BindProperty]
        [HiddenInput]
        public string ProviderSnapshotId { get; set; }

        [BindProperty]
        [HiddenInput]
        public int PaymentOrganisationId { get; set; }
    }
}
