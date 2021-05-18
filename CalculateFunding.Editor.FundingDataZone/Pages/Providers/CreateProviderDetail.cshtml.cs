using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Services.FundingDataZone;
using CalculateFunding.Services.FundingDataZone.SqlModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CalculateFunding.Editor.FundingDataZone.Pages.Provider
{
    public class CreateProviderDetailModel : PageModel
    {
        private readonly IPublishingAreaEditorRepository _repo;

        public CreateProviderDetailModel(IPublishingAreaEditorRepository publishingAreaEditorRepository)
        {
            _repo = publishingAreaEditorRepository;
        }

        public async Task<IActionResult> OnGet([FromRoute] int providerSnapshotId)
        {
            PaymentOrganisations = await _repo.GetAllOrganisations(providerSnapshotId);
            Providers = await _repo.GetProvidersInSnapshot(providerSnapshotId);
            Statuses = await _repo.GetProviderStatuses();

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            IEnumerable<PublishingAreaOrganisation> paymentOrganisations  = await _repo.GetAllOrganisations(Convert.ToInt32(Provider.ProviderSnapshotId));
            IEnumerable<ProviderStatus> statuses = await _repo.GetProviderStatuses();

            Provider.ProviderId = Provider.UKPRN;

            if (Provider.Status != null)
            {
                var providerStatus = statuses.SingleOrDefault(_ => _.ProviderStatusName == Provider.Status);
                if (providerStatus == null)
                {
                    ModelState.AddModelError("Provider.Status", "Invalid provider status");
                }
                else
                {
                    Provider.ProviderStatusId = providerStatus.ProviderStatusId;
                }
            }
            else
            {
                Provider.ProviderStatusId = null;
            }
            
            if (Provider.PaymentOrganisationUkprn != null)
            {
                var organisation = paymentOrganisations.SingleOrDefault(_ => _.Ukprn == Provider.PaymentOrganisationUkprn);
                if (organisation == null)
                {
                    ModelState.AddModelError("Provider.PaymentOrganisationUkprn", "Invalid organisation");
                }
                else
                {
                    Provider.PaymentOrganisationId = organisation.PaymentOrganisationId;
                }
            }
            else
            {
                Provider.PaymentOrganisationId = null;
            }

            if (!ModelState.IsValid)
            {
                PaymentOrganisations = await _repo.GetAllOrganisations(Convert.ToInt32(Provider.ProviderSnapshotId));
                Providers = await _repo.GetProvidersInSnapshot(Convert.ToInt32(Provider.ProviderSnapshotId));
                Statuses = await _repo.GetProviderStatuses();

                return await Task.FromResult<IActionResult>(null);
            }

            Provider = await _repo.InsertProvider(Provider);
            
            if (!string.IsNullOrWhiteSpace(Provider.Predecessors))
            {
                await _repo.CreatePredecessors(Provider.Predecessors.Split(',').Select(_ => new Predecessor { ProviderId = Convert.ToInt32(Provider.Id), UKPRN = _ }));
            }

            if (!string.IsNullOrWhiteSpace(Provider.Successors))
            {
                await _repo.CreateSuccessors(Provider.Successors.Split(',').Select(_ => new Successor { ProviderId = Convert.ToInt32(Provider.Id), UKPRN = _ }));
            }

            return RedirectToPage("/Providers/ProviderDetail", new { providerSnapshotId = Provider.ProviderSnapshotId, ProviderId = Provider.ProviderId });
        }

        [BindProperty]
        public PublishingAreaProvider Provider { get; set; }

        public IEnumerable<PublishingAreaProvider> Providers { get; set; }

        public IEnumerable<PublishingAreaOrganisation> PaymentOrganisations { get; set; }

        public IEnumerable<ProviderStatus> Statuses { get; set; }
    }
}
