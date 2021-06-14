using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Services.FundingDataZone;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using CalculateFunding.Services.FundingDataZone.SqlModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CalculateFunding.Editor.FundingDataZone.Pages.ProviderSnapshots
{
    public class IndexModel : PageModel
    {
        private readonly IPublishingAreaEditorRepository _repo;
        private readonly IProviderRetrievalService _providerRetrievalService;

        public IndexModel(IPublishingAreaEditorRepository publishingAreaEditorRepository,
            IProviderRetrievalService providerRetrievalService)
        {
            _repo = publishingAreaEditorRepository;
            _providerRetrievalService = providerRetrievalService;
        }

        public IEnumerable<PublishingAreaProviderSnapshot> Snapshots { get; private set; }

        public bool DisableToggleLatest { get; set; }

        public bool FilteredByFundingStream { get; set; }

        public async Task<IActionResult> OnGet([FromQuery] int? fundingStreamId = null)
        {
            DisableToggleLatest = await _providerRetrievalService.GetDisableTrackLatest();

            if (!fundingStreamId.HasValue)
            {
                Snapshots = await _repo.GetProviderSnapshotsOrderedByTargetDate();
                FilteredByFundingStream = false;
            }
            else
            {
                Snapshots = await _repo.GetProviderSnapshotsOrderedByTargetDate(fundingStreamId.Value);
                FilteredByFundingStream = true;
            }

            return new PageResult();
        }
    }
}
