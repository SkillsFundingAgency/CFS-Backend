using CalculateFunding.Services.FundingDataZone;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Editor.FundingDataZone.Controllers
{
    public class CloneController : Controller
    {
        private readonly IPublishingAreaEditorRepository _repo;
        private readonly IProviderRetrievalService _providerRetrievalService;

        public CloneController(IPublishingAreaEditorRepository publishingAreaEditorRepository,
            IProviderRetrievalService providerRetrievalService)
        {
            _repo = publishingAreaEditorRepository;
            _providerRetrievalService = providerRetrievalService;
        }

        [Route("cloning/{providerSnapshotId}/{cloneName}")]
        [HttpGet]
        public async Task<IActionResult> Clone([FromRoute] int providerSnapshotId, [FromRoute] string cloneName)
        {
            int cloneSnapShotId = await _repo.CloneProviderSnapshot(providerSnapshotId, cloneName);
            return RedirectToPage("/ProviderSnapshots/ProviderSnapshotDetails", new { providerSnapshotId  = cloneSnapShotId });
        }

        [Route("cloning/disableTrackLatest/{disableTrackLatest}")]
        [HttpGet]
        public async Task DisableTrackLatest([FromRoute] bool disableTrackLatest)
        {
            await _providerRetrievalService.DisableTrackLatest(disableTrackLatest);
        }
    }
}
