using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.FundingDataZone;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CalculateFunding.Api.FundingDataZone.Controllers
{
    [ApiController]
    public class ProviderMetadataController : ControllerBase
    {
        private readonly IFundingStreamsWithProviderSnapshotsRetrievalService _fundingStreamsWithProviderSnapshotsRetrievalService;
        private readonly IProviderSnapshotMetadataRetrievalService _providerSnapshotMetadataRetrievalService;
        private readonly IProviderSnapshotForFundingStreamService _providerSnapshotForFundingStreamService;
        private readonly IProviderSnapshotFundingPeriodService _providerSnapshotFundingPeriodService;

        public ProviderMetadataController(
            IFundingStreamsWithProviderSnapshotsRetrievalService fundingStreamsWithProviderSnapshotsRetrievalService,
            IProviderSnapshotForFundingStreamService providerSnapshotForFundingStreamService,
            IProviderSnapshotMetadataRetrievalService providerSnapshotMetadataRetrievalService,
            IProviderSnapshotFundingPeriodService providerSnapshotFundingPeriodService)
        {
            Guard.ArgumentNotNull(fundingStreamsWithProviderSnapshotsRetrievalService, nameof(fundingStreamsWithProviderSnapshotsRetrievalService));
            Guard.ArgumentNotNull(providerSnapshotMetadataRetrievalService, nameof(providerSnapshotMetadataRetrievalService));
            Guard.ArgumentNotNull(providerSnapshotForFundingStreamService, nameof(providerSnapshotForFundingStreamService));
            Guard.ArgumentNotNull(providerSnapshotFundingPeriodService, nameof(providerSnapshotFundingPeriodService));

            _fundingStreamsWithProviderSnapshotsRetrievalService = fundingStreamsWithProviderSnapshotsRetrievalService;
            _providerSnapshotForFundingStreamService = providerSnapshotForFundingStreamService;
            _providerSnapshotMetadataRetrievalService = providerSnapshotMetadataRetrievalService;
            _providerSnapshotFundingPeriodService = providerSnapshotFundingPeriodService;
        }

        private const string ListFundingStreamsWithProviderSnapshotsDescription = @"
```
Given I am CFS user
When I list funding streams which have provider snapshots
Then I am able to see the funding stream ID where snapshots exist
```
Used as input for:

- Querying the get provider snapshots by funding stream
";

        /// <summary>
        /// List funding streams with a provider snapshots
        /// </summary>
        /// <returns></returns>
        [SwaggerOperation(Summary = "List funding streams with one or more provider snapshots", Description = ListFundingStreamsWithProviderSnapshotsDescription)]
        [HttpGet("api/providers/fundingStreams")]
        [Produces(typeof(IEnumerable<string>))]
        public async Task<ActionResult<IEnumerable<string>>> ListFundingStreamsWithProviderSnapshots()
        {
            IEnumerable<string> fundingStreams = 
                await _fundingStreamsWithProviderSnapshotsRetrievalService.GetFundingStreamsWithProviderSnapshots();

            if (!fundingStreams.Any())
            {
                return new NotFoundObjectResult(new ProblemDetails() { Title = "No funding streams with provider snapshots found" });
            }

            return new OkObjectResult(fundingStreams);
        }

        private const string GetProviderSnapshotsForFundingStreamDescription = @"
        Given I am CFS user
        When I browse available provider snapshots for a funding stream
        Then I am able to see the snapshot display name
        And I am able to see the description for that snapshot
        And I am able to determine which date the snapshot data was targeted at
        And I am able to the version of the snapshot based on the target date";

        [SwaggerOperation(Summary = "List all provider snapshots for a given funding stream", Description = GetProviderSnapshotsForFundingStreamDescription)]
        [HttpGet("api/providers/fundingStreams/{fundingStreamId}/snapshots")]
        [Produces(typeof(IEnumerable<ProviderSnapshot>))]
        public async Task<ActionResult<IEnumerable<ProviderSnapshot>>> GetProviderSnapshotsForFundingStream(
            [FromRoute]string fundingStreamId)
        {
            IEnumerable<ProviderSnapshot> providerSnapshots = 
                await _providerSnapshotForFundingStreamService.GetProviderSnapshotsForFundingStream(fundingStreamId);
            return new OkObjectResult(providerSnapshots);
        }

        private const string GetLatestProviderSnapshotsForFundingStreamsDescription = @"
        Given I am CFS user
        When I browse latest provider snapshots for funding streams
        Then I am able to see all latest provider snapshots for all funding streams
        And I am able to see the snapshot display names
        And I am able to see the description for those snapshots
        And I am able to determine which date the snapshot data was targeted at
        And I am able to see the version of the snapshot based on the target date";
        /// <summary>
        ///  Get latest provider snapshots for all funding streams
        /// </summary>
        /// <returns>Returns ProviderSnapshots</returns>
        [SwaggerOperation(Summary = "List all provider snapshots for all funding streams", Description = GetLatestProviderSnapshotsForFundingStreamsDescription)]
        [HttpGet("api/providers/fundingStreams/snapshots/latest")]
        [Produces(typeof(IEnumerable<ProviderSnapshot>))]
        public async Task<ActionResult<IEnumerable<ProviderSnapshot>>> GetLatestProviderSnapshotsForAllFundingStreams()
        {
            IEnumerable<ProviderSnapshot> providerSnapshots =
                await _providerSnapshotForFundingStreamService.GetLatestProviderSnapshotsForAllFundingStreams();
            return new OkObjectResult(providerSnapshots);
        }

        private const string GetProviderSnapshotMetadataDescription = @"
        ```
        Given I am CFS user
        And I provide a valid provider snapshot ID
        When I request information about a particular provider snapshot
        Then I am able to see all details about the provider snapshot
        ```
        Used as input for:

        - Browsing provider snapshots
        ";

        /// <summary>
        /// Get provider snapshot metadata
        /// </summary>
        /// <param name="providerSnapshotId">Provider Snapshot Id</param>
        /// <returns></returns>
        [SwaggerOperation(Summary = "Get provider snapshot metadata", Description = GetProviderSnapshotMetadataDescription)]
        [HttpGet("api/providers/snapshots/{providerSnapshotId}")]
        [Produces(typeof(IEnumerable<Provider>))]
        public async Task<ActionResult<IEnumerable<Provider>>> GetProviderSnapshotMetadata(
            [FromRoute] int providerSnapshotId)
        {
            ProviderSnapshot snapshot = await _providerSnapshotMetadataRetrievalService.GetProviderSnapshotsMetadata(providerSnapshotId);

            if (snapshot == null)
            {
                return new NotFoundObjectResult(new ProblemDetails
                {
                    Title = "No provider snapshot shot found for providerSnapshotId"
                });
            }

            return new OkObjectResult(snapshot);
        }

        private const string PopulateProviderSnapshotFundingPeriodDescription = @"
        ```
        Given I am CFS user
        When I call populate funding period for given provider snapshot
        Then I am able to see funding period of given provider snapshot
        ```
        Used as input for:

        - Browsing provider snapshot funding period
        ";

        /// <summary>
        /// Populate provider snapshot funding periods
        /// </summary>
        /// <returns></returns>
        [SwaggerOperation(Summary = "Populate provider snapshot funding period metadata", Description = PopulateProviderSnapshotFundingPeriodDescription)]
        [HttpGet("api/providers/snapshots/{providerSnapshotId}/populate-funding-periods")]
        public async Task<ActionResult> PopulateProviderSnapshotFundingPeriod(
            [FromRoute] int providerSnapshotId)
        {
            await _providerSnapshotFundingPeriodService.PopulateFundingPeriod(providerSnapshotId);

            return new OkResult();
        }

        private const string PopulateProviderSnapshotFundingPeriodsDescription = @"
        ```
        Given I am CFS user
        When I call populate funding period for all provider snapshots
        Then I am able to see funding period of all provider snapshots
        ```
        Used as input for:

        - Browsing provider snapshot funding period
        ";

        /// <summary>
        /// Populate provider snapshot funding periods
        /// </summary>
        /// <returns></returns>
        [SwaggerOperation(Summary = "Populate provider snapshots funding period metadata", Description = PopulateProviderSnapshotFundingPeriodsDescription)]
        [HttpGet("api/providers/snapshots/populate-funding-periods")]
        public async Task<ActionResult> PopulateProviderSnapshotFundingPeriods()
        {
            await _providerSnapshotFundingPeriodService.PopulateFundingPeriods();

            return new OkResult();
        }
    }
}
