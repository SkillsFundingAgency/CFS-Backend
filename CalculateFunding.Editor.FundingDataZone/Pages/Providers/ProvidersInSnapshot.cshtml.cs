using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Services.FundingDataZone.Interfaces;
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
            SearchTerm = searchTerm;
            TotalPages = Convert.ToInt32(Math.Ceiling(decimal.Divide(await _repo.GetCountProvidersInSnapshot(providerSnapshotId, SearchTerm), pageSize)));

            Providers = await _repo.GetProvidersInSnapshot(providerSnapshotId, pageNumber, pageSize, SearchTerm);

            return Page();
        }

        public IEnumerable<ProviderSummary> Providers { get; set; }

        public string SearchTerm { get; set; }
    }
}
