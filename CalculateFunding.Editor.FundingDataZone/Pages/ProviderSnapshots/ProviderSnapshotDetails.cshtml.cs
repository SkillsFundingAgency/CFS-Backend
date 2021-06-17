using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Services.FundingDataZone;
using CalculateFunding.Services.FundingDataZone.SqlModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CalculateFunding.Editor.FundingDataZone.Pages.ProviderSnapshots
{
    public class ProviderSnapshotDetailsModel : PageModel
    {
        private readonly IPublishingAreaEditorRepository _repo;

        public ProviderSnapshotDetailsModel(IPublishingAreaEditorRepository publishingAreaEditorRepository)
        {
            _repo = publishingAreaEditorRepository;
        }

        public async Task<IActionResult> OnGet([FromRoute] int providerSnapshotId, [FromQuery] int pageNumber = 1, [FromQuery] string searchTerm = null)
        {
            PublishingAreaProviderSnapshot providerAreaSnapshot = await _repo.GetProviderSnapshotMetadata(providerSnapshotId);

            int fundingStreamId = (await PopulateFundingStreams()).Single(_ => _.FundingStreamCode == providerAreaSnapshot.FundingStreamCode).FundingStreamId;

            Created = providerAreaSnapshot.Created;
            ProviderSnapshotId = providerAreaSnapshot.ProviderSnapshotId.ToString();

            ProviderSnapshot = new ProviderSnapshotRequest
            {
                Name = providerAreaSnapshot.Name,
                Description = providerAreaSnapshot.Description,
                FundingStreamId = fundingStreamId,
                TargetDate = providerAreaSnapshot.TargetDate,
                Version = providerAreaSnapshot.Version
            };

            return Page();
        }

        private async Task<IEnumerable<FundingStream>> PopulateFundingStreams()
        {
            var fundingStreams = await _repo.GetFundingStreams();

            FundingStreams = fundingStreams.Select(_ => new SelectListItem(_.FundingStreamName, _.FundingStreamId.ToString()));

            return fundingStreams;
        }

        public async Task<IActionResult> OnPost()
        {
            if (!ModelState.IsValid)
            {
                await PopulateFundingStreams();
                return Page();
            }

            ProviderSnapshotTableModel providerSnapshotTableModel = new ProviderSnapshotTableModel
            {
                Name = ProviderSnapshot.Name,
                ProviderSnapshotId = Convert.ToInt32(ProviderSnapshotId),
                Created = Convert.ToDateTime(Created),
                Description = ProviderSnapshot.Description,
                FundingStreamId = ProviderSnapshot.FundingStreamId.Value,
                TargetDate = ProviderSnapshot.TargetDate,
                Version = ProviderSnapshot.Version
            };

            if (await _repo.UpdateProviderSnapshot(providerSnapshotTableModel))
            {
                return RedirectToPage("/ProviderSnapshots/ProviderSnapshotDetails", new { providerSnapshotId = ProviderSnapshotId });
            }

            return Page();
        }

        [BindProperty]
        public ProviderSnapshotRequest ProviderSnapshot { get; set; }

        [BindProperty]
        [HiddenInput]
        public DateTime Created { get; set; }

        [BindProperty]
        [HiddenInput]
        public string ProviderSnapshotId { get; set; }

        public IEnumerable<SelectListItem> FundingStreams { get; set; }
    }
}
