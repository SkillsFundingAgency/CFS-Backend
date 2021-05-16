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
    public class ProviderDetailModel : PageModel
    {
        private readonly IPublishingAreaEditorRepository _repo;

        public ProviderDetailModel(IPublishingAreaEditorRepository publishingAreaEditorRepository)
        {
            _repo = publishingAreaEditorRepository;
        }

        public async Task<IActionResult> OnGet([FromRoute] int providerSnapshotId, [FromRoute] string providerId)
        {
            Provider = await _repo.GetProviderInSnapshot(providerSnapshotId, providerId);
            PaymentOrganisations = await _repo.GetAllOrganisations(providerSnapshotId);
            Providers = await _repo.GetProvidersInSnapshot(providerSnapshotId);
            Statuses = await _repo.GetProviderStatuses();

            ProviderSnapshotId = providerSnapshotId.ToString();
            ProviderId = providerId;

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            PublishingAreaProvider provider  = await _repo.GetProviderInSnapshot(Convert.ToInt32(ProviderSnapshotId), ProviderId);
            IEnumerable<PublishingAreaOrganisation> paymentOrganisations  = await _repo.GetAllOrganisations(Convert.ToInt32(ProviderSnapshotId));
            IEnumerable<ProviderStatus> statuses = await _repo.GetProviderStatuses();

            provider.Name = Provider.Name;
            provider.URN = Provider.URN;
            provider.UKPRN = Provider.UKPRN;
            provider.UPIN = Provider.UPIN;
            provider.EstablishmentNumber = Provider.EstablishmentNumber;
            provider.DfeEstablishmentNumber = Provider.DfeEstablishmentNumber;
            provider.Authority = Provider.Authority;
            provider.ProviderType = Provider.ProviderType;
            provider.ProviderSubType = Provider.ProviderSubType;
            provider.DateOpened = Provider.DateOpened;
            provider.DateClosed = Provider.DateClosed;
            provider.LACode = Provider.LACode;
            provider.NavVendorNo = Provider.NavVendorNo;
            provider.CrmAccountId = Provider.CrmAccountId;
            provider.LegalName = Provider.LegalName;
            provider.Status = Provider.Status;
            provider.PhaseOfEducation = Provider.PhaseOfEducation;
            provider.ReasonEstablishmentOpened = Provider.ReasonEstablishmentOpened;
            provider.ReasonEstablishmentClosed = Provider.ReasonEstablishmentClosed;
            provider.Successor = Provider.Successor;
            provider.Town = Provider.Town;
            provider.Postcode = Provider.Postcode;
            provider.TrustName = Provider.TrustName;
            provider.TrustCode = Provider.TrustCode;
            provider.LocalAuthorityName = Provider.LocalAuthorityName;
            provider.CompaniesHouseNumber = Provider.CompaniesHouseNumber;
            provider.GroupIdNumber = Provider.GroupIdNumber;
            provider.RscRegionName = Provider.RscRegionName;
            provider.RscRegionCode = Provider.RscRegionCode;
            provider.GovernmentOfficeRegionName = Provider.GovernmentOfficeRegionName;
            provider.GovernmentOfficeRegionCode = Provider.GovernmentOfficeRegionCode;
            provider.DistrictName = Provider.DistrictName;
            provider.DistrictCode = Provider.DistrictCode;
            provider.WardName = Provider.WardName;
            provider.WardCode = Provider.WardCode;
            provider.CensusWardName = Provider.CensusWardName;
            provider.CensusWardCode = Provider.CensusWardCode;
            provider.MiddleSuperOutputAreaName = Provider.MiddleSuperOutputAreaName;
            provider.MiddleSuperOutputAreaCode = Provider.MiddleSuperOutputAreaCode;
            provider.LowerSuperOutputAreaName = Provider.LowerSuperOutputAreaName;
            provider.LowerSuperOutputAreaCode = Provider.LowerSuperOutputAreaCode;
            provider.ParliamentaryConstituencyName = Provider.ParliamentaryConstituencyName;
            provider.ParliamentaryConstituencyCode = Provider.ParliamentaryConstituencyCode;
            provider.CountryCode = Provider.CountryCode;
            provider.CountryName = Provider.CountryName;
            provider.LocalGovernmentGroupTypeCode = Provider.LocalGovernmentGroupTypeCode;
            provider.LocalGovernmentGroupTypeName = Provider.LocalGovernmentGroupTypeName;
            provider.TrustStatus = Provider.TrustStatus;
            provider.ProviderTypeCode = Provider.ProviderTypeCode;
            provider.ProviderSubTypeCode = Provider.ProviderSubTypeCode;
            provider.StatusCode = Provider.StatusCode;
            provider.ReasonEstablishmentOpenedCode = Provider.ReasonEstablishmentOpenedCode;
            provider.ReasonEstablishmentClosedCode = Provider.ReasonEstablishmentClosedCode;
            provider.PhaseOfEducationCode = Provider.PhaseOfEducationCode;
            provider.StatutoryLowAge = Provider.StatutoryLowAge;
            provider.StatutoryHighAge = Provider.StatutoryHighAge;
            provider.OfficialSixthFormCode = Provider.OfficialSixthFormCode;
            provider.OfficialSixthFormName = Provider.OfficialSixthFormName;
            provider.PreviousLaCode = Provider.PreviousLaCode;
            provider.PreviousLaName = Provider.PreviousLaName;
            provider.PreviousEstablishmentNumber = Provider.PreviousEstablishmentNumber;
            provider.Indicative = Provider.Indicative;
            provider.Status = Provider.Status;
            provider.StatusCode = Provider.StatusCode;
            
            if (Provider.Status != null)
            {
                var providerStatus = statuses.SingleOrDefault(_ => _.ProviderStatusName == Provider.Status);
                if (providerStatus == null)
                {
                    ModelState.AddModelError("Provider.Status", "Invalid provider status");
                }
                else
                {
                    provider.ProviderStatusId = providerStatus.ProviderStatusId;
                }
            }
            else
            {
                provider.ProviderStatusId = null;
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
                    provider.PaymentOrganisationId = organisation.PaymentOrganisationId;
                }
            }
            else
            {
                provider.PaymentOrganisationId = null;
            }

            if (!ModelState.IsValid)
            {
                PaymentOrganisations = await _repo.GetAllOrganisations(Convert.ToInt32(ProviderSnapshotId));
                Providers = await _repo.GetProvidersInSnapshot(Convert.ToInt32(ProviderSnapshotId));
                Statuses = await _repo.GetProviderStatuses();

                return await Task.FromResult<IActionResult>(null);
            }

            await _repo.DeletePredecessors(Convert.ToInt32(provider.Id));
            if (!string.IsNullOrWhiteSpace(Provider.Predecessors))
            {
                await _repo.CreatePredecessors(Provider.Predecessors.Split(',').Select(_ => new Predecessor { ProviderId = Convert.ToInt32(provider.Id), UKPRN = _ }));
            }

            await _repo.DeleteSuccessors(Convert.ToInt32(provider.Id));
            if (!string.IsNullOrWhiteSpace(Provider.Successors))
            {
                await _repo.CreateSuccessors(Provider.Successors.Split(',').Select(_ => new Successor { ProviderId = Convert.ToInt32(provider.Id), UKPRN = _ }));
            }

            if (await _repo.UpdateProvider(provider))
            {
                return RedirectToPage("/Providers/ProviderDetail", new { providerSnapshotId = provider.ProviderSnapshotId, ProviderId = provider.ProviderId });
            }

            return await Task.FromResult<IActionResult>(null);
        }

        [BindProperty]
        public PublishingAreaProvider Provider { get; set; }

        public IEnumerable<PublishingAreaOrganisation> PaymentOrganisations { get; set; }

        public IEnumerable<PublishingAreaProvider> Providers { get; set; }

        public IEnumerable<ProviderStatus> Statuses { get; set; }

        [BindProperty]
        [HiddenInput]
        public string ProviderSnapshotId { get; set; }

        [BindProperty]
        [HiddenInput]
        public string ProviderId { get; set; }
    }
}
