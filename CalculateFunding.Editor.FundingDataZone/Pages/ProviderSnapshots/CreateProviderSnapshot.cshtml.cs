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
    public class CreateProviderSnapshotModel : PageModel
    {
        private readonly IPublishingAreaEditorRepository _repo;

        public CreateProviderSnapshotModel(IPublishingAreaEditorRepository publishingAreaEditorRepository)
        {
            _repo = publishingAreaEditorRepository;
        }

        public async Task<IActionResult> OnGet()
        {
            await PopulateFundingStreams();

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            if (!ModelState.IsValid)
            {
                await PopulateFundingStreams();
                return Page();
            }

            ProviderSnapshotTableModel providerSnapshot = new ProviderSnapshotTableModel()
            {
                Created = DateTime.UtcNow,
                Description = ProviderSnapshot.Description,
                FundingStreamId = ProviderSnapshot.FundingStreamId.Value,
                Name = ProviderSnapshot.Name,
                TargetDate = ProviderSnapshot.TargetDate,
                Version = ProviderSnapshot.Version,
            };

            ProviderSnapshotTableModel createdItem = await _repo.CreateProviderSnapshot(providerSnapshot);

            return RedirectToPage("/ProviderSnapshots/ProviderSnapshotDetails", new { providerSnapshotId = createdItem.ProviderSnapshotId });
        }

        private async Task PopulateFundingStreams()
        {
            var fundingStreams = await _repo.GetFundingStreams();

            FundingStreams = fundingStreams.Select(_ => new SelectListItem(_.FundingStreamName, _.FundingStreamId.ToString()));
        }

        [BindProperty]
        public ProviderSnapshotRequest ProviderSnapshot { get; set; }

        public IEnumerable<SelectListItem> FundingStreams { get; set; }
    }
}
