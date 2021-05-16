using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Services.FundingDataZone;
using CalculateFunding.Services.FundingDataZone.SqlModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CalculateFunding.Editor.FundingDataZone.Pages.Providers
{
    public class ProvidersInSnapshotModel : PageModel
    {
        private readonly IPublishingAreaEditorRepository _repo;

        public int CurrentPage;
        public int TotalPages;

        public ProvidersInSnapshotModel(IPublishingAreaEditorRepository publishingAreaEditorRepository)
        {
            _repo = publishingAreaEditorRepository;
        }

        public async Task<IActionResult> OnGet([FromRoute] int providerSnapshotId, [FromQuery] int pageNumber = 1, [FromQuery] string searchTerm = null)
        {
            int pageSize = 100;
            CurrentPage = pageNumber;
            TotalPages = Convert.ToInt32(Math.Ceiling(decimal.Divide(await _repo.GetCountProvidersInSnapshot(providerSnapshotId),pageSize)));

            Providers = await _repo.GetProvidersInSnapshot(providerSnapshotId, pageNumber, pageSize, searchTerm);

            return Page();
        }

        public IEnumerable<ProviderSummary> Providers { get; set; }
    }
}
