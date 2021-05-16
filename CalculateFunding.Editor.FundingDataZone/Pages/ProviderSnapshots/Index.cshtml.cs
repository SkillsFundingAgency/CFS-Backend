using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Services.FundingDataZone;
using CalculateFunding.Services.FundingDataZone.SqlModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CalculateFunding.Editor.FundingDataZone.Pages.ProviderSnapshots
{
    public class IndexModel : PageModel
    {
        private readonly IPublishingAreaEditorRepository _repo;

        public IndexModel(IPublishingAreaEditorRepository publishingAreaEditorRepository)
        {
            _repo = publishingAreaEditorRepository;
        }

        public IEnumerable<PublishingAreaProviderSnapshot> Snapshots { get; private set; }

        public bool FilteredByFundingStream { get; set; }

        public async Task<IActionResult> OnGet([FromQuery] int? fundingStreamId = null)
        {
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
