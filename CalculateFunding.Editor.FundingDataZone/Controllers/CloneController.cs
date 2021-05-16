using CalculateFunding.Editor.FundingDataZone.Pages.ProviderSnapshots;
using CalculateFunding.Services.FundingDataZone;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Editor.FundingDataZone.Controllers
{
    public class CloneController : Controller
    {
        private readonly IPublishingAreaEditorRepository _repo;
        
        public CloneController(IPublishingAreaEditorRepository publishingAreaEditorRepository)
        {
            _repo = publishingAreaEditorRepository;
        }

        [Route("cloning/{providerSnapshotId}/{cloneName}")]
        [HttpGet]
        public async Task<IActionResult> Clone([FromRoute] int providerSnapshotId, [FromRoute] string cloneName)
        {
            int cloneSnapShotId = await _repo.CloneProviderSnapshot(providerSnapshotId, cloneName);
            return RedirectToPage("/ProviderSnapshots/ProviderSnapshotDetails", new { providerSnapshotId  = cloneSnapShotId });
        }
    }
}
