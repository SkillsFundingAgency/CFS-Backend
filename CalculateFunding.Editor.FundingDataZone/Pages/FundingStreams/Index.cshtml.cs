using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using CalculateFunding.Services.FundingDataZone.SqlModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CalculateFunding.Editor.FundingDataZone.Pages.FundingStreams
{
    public class IndexModel : PageModel
    {
        private readonly IPublishingAreaEditorRepository _repo;

        public IndexModel(IPublishingAreaEditorRepository publishingAreaEditorRepository)
        {
            _repo = publishingAreaEditorRepository;
        }

        public IEnumerable<FundingStream> FundingStreams { get; private set; }

        public async Task<IActionResult> OnGet()
        {
            FundingStreams = (await _repo.GetFundingStreams()).OrderBy(_ => _.FundingStreamName);

            return new PageResult();
        }
    }
}
