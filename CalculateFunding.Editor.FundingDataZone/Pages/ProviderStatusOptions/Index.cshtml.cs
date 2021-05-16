using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Services.FundingDataZone;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CalculateFunding.Editor.FundingDataZone.Pages.ProviderStatusOptions
{
    public class IndexModel : PageModel
    {
        private readonly IPublishingAreaEditorRepository _repo;

        public IndexModel(IPublishingAreaEditorRepository publishingAreaEditorRepository)
        {
            _repo = publishingAreaEditorRepository;
        }

        public IEnumerable<CalculateFunding.Services.FundingDataZone.SqlModels.ProviderStatus> Statuses { get; private set; }

        public async Task<IActionResult> OnGet()
        {
            Statuses = await _repo.GetProviderStatuses();

            return new PageResult();
        }
    }
}
