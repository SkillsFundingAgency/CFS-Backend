using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using CalculateFunding.Services.FundingDataZone.SqlModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CalculateFunding.Editor.FundingDataZone.Pages.PaymentOrganisations
{
    public class IndexModel : PageModel
    {
        private readonly IPublishingAreaEditorRepository _repo;

        public int CurrentPage;
        public int TotalPages;

        public IEnumerable<PublishingAreaOrganisation> Organisations { get; private set; }

        public IndexModel(IPublishingAreaEditorRepository publishingAreaEditorRepository)
        {
            _repo = publishingAreaEditorRepository;
        }

        public async Task<IActionResult> OnGet([FromRoute] int? providerSnapshotId = null, [FromQuery] int pageNumber = 1, [FromQuery] string searchTerm = null)
        {
            int pageSize = 100;
            CurrentPage = pageNumber;
            SearchTerm = searchTerm;
            TotalPages = Convert.ToInt32(Math.Ceiling(decimal.Divide(await _repo.GetCountPaymentOrganisationsInSnapshot(providerSnapshotId.Value, searchTerm), pageSize)));

            Organisations = await _repo.GetPaymentOrganisationsInSnapshot(providerSnapshotId.Value, pageNumber, pageSize, searchTerm);

            return new PageResult();
        }

        public string SearchTerm { get; set; }
    }
}
