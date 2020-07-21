using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.FDZ;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CalculateFunding.Api.FundingDataZone.Controllers
{
    [ApiController]
    public class DatasetListingController : ControllerBase
    {
        private readonly IFundingStreamsWithDatasetsService _fundingStreamsWithDatasetsService;
        private readonly IDatasetsForFundingStreamService _datasetsForFundingStreamService;

        public DatasetListingController(
            IFundingStreamsWithDatasetsService fundingStreamsWithDatasetsService,
            IDatasetsForFundingStreamService datasetsForFundingStreamService)
        {
            _fundingStreamsWithDatasetsService = fundingStreamsWithDatasetsService;
            _datasetsForFundingStreamService = datasetsForFundingStreamService;
        }

        private const string GetFundingStreamsWithDatsetsDescription = @"
This API will return a list of funding stream IDs (stored in the FundingStreamId extended property)
```
Given I am CFS user
When I want to browse datasets for funding streams
Then I am able to see all funding streams with datasets
```
";

        /// <summary>
        /// List a distinct list of funding streams with datasets in the publishing area. 
        /// </summary>
        /// <returns>A distinct list of funding stream IDs based on created datasets in publishing area</returns>
        [HttpGet("api/datasets/fundingStreams")]
        [SwaggerOperation(
            Summary = "List a distinct list of funding streams with datasets in the publishing area", 
            Description = GetFundingStreamsWithDatsetsDescription)]
        [Produces(typeof(IEnumerable<string>))]
        public async Task<ActionResult<IEnumerable<string>>> GetFundingStreamsWithDatasets()
        {
            var fundingStreamIds = await _fundingStreamsWithDatasetsService.GetFundingStreamsWithDatasets();
            return new OkObjectResult(fundingStreamIds);
        }

        private const string GetDatasetsAndVersionsForFundingStreamDescription = @"
```
Given I am a CFS user
And one or more datasets have been published
When I browse available datasets within a funding stream
Then I am able to see a list of all datasets
And I am able to see each version created in the publishing area
And I am able to see the metadata associated with each dataset
```
";

        /// <summary>
        /// List all datasets and versions for a funding stream.
        /// </summary>
        /// <param name="fundingStreamId">Funding Stream ID</param>
        /// <returns></returns>
        [HttpGet("api/datasets/fundingStreams/{fundingStreamId}")]
        [SwaggerOperation(
            Summary = "List all datasets and versions for a funding stream", 
            Description = GetDatasetsAndVersionsForFundingStreamDescription)]
        [Produces(typeof(IEnumerable<Dataset>))]
        public async Task<ActionResult<IEnumerable<Dataset>>> GetDatasetsAndVersionsForFundingStream(
            [FromRoute] string fundingStreamId)
        {
            IEnumerable<Dataset> datasets = 
                await _datasetsForFundingStreamService.GetDatasetsForFundingStream(fundingStreamId);

            return new OkObjectResult(datasets);
        }

        private const string GetDatasetsForFundingStreamDescription = @"
Returns a distinct list of DatasetCode's which are available for this funding stream.

```
Given I am a CFS user
When I view a funding stream
Then I am able to view distinct list of dataset codes which have available versions
 ```

TODO: Work a display name for a dataset, there are currently only verisons, no individual top level entity.
Can the name come from the latest dataset version?
";

        /// <summary>
        /// Get distinct datasets for a funding stream
        /// </summary>
        /// <param name="fundingStreamId">Funding Stream ID</param>
        /// <returns>Dataset version metadata and fields within this dataset</returns>
        [HttpGet("api/datasets/fundingStreams/{fundingStreamId}/datasets")]
        [SwaggerOperation(Summary = "Get distinct datasets for a funding stream", Description = GetDatasetsForFundingStreamDescription)]
        [Produces(typeof(IEnumerable<DatasetMetadata>))]
        public async Task<ActionResult<IEnumerable<DatasetMetadata>>> GetDatasetsForFundingStream(
            [FromRoute] string fundingStreamId)
        {
            return await Task.FromResult(new OkObjectResult(Enumerable.Empty<DatasetMetadata>()));
        }

        private const string GetDatasetVersionsForDatasetDescription = @"
```
Given I am a CFS user
When I view a funding stream
Then I am able to view all of the dataset version metadata
And I am able to see all fields for this dataset
And I am able to see the name and data type of each field
And I am able to determine which colum contains the identifer based on the identifier type
And I am able to determine which column contains the row ID
 ```
";

        /// <summary>
        /// Get dataset versions for a dataset
        /// </summary>
        /// <param name="fundingStreamId">Funding Stream ID</param>
        /// <param name="datasetCode">Dataset Code</param>
        /// <returns>Dataset version metadata and fields within this dataset</returns>
        [HttpGet("api/datasets/fundingStreams/{fundingStreamId}/datasets/{datasetCode}")]
        [SwaggerOperation(Summary = "Get dataset versions for a dataset", Description = GetDatasetVersionsForDatasetDescription)]
        [Produces(typeof(IEnumerable<DatasetMetadata>))]
        public async Task<ActionResult<IEnumerable<DatasetMetadata>>> GetDatasetVersionsForDataset(
            [FromRoute] string fundingStreamId,
            [FromRoute] string datasetCode)
        {
            return await Task.FromResult(new OkObjectResult(Enumerable.Empty<DatasetMetadata>()));
        }


        private const string GetDatasetMetadataForDatasetDescription = @"
```
Given I am a CFS user
When I view a funding stream
Then I am able to view all of the dataset version metadata
And I am able to see all fields for this dataset
And I am able to see the name and data type of each field
And I am able to determine which colum contains the identifer based on the identifier type
And I am able to determine which column contains the row ID
 ```
";

        /// <summary>
        /// Get the metadata for a single dataset version.
        /// </summary>
        /// <param name="fundingStreamId">Funding Stream ID</param>
        /// <param name="datasetCode">Dataset Code</param>
        /// <param name="versionNumber">Version Number</param>
        /// <returns>Dataset version metadata and fields within this dataset</returns>
        [HttpGet("api/datasets/fundingStreams/{fundingStreamId}/datasets/{datasetCode}/{versionNumber}")]
        [SwaggerOperation(Summary = "Get the metadata for a single dataset version", Description = GetDatasetMetadataForDatasetDescription)]
        [Produces(typeof(IEnumerable<Dataset>))]
        public async Task<ActionResult<DatasetMetadata>> GetDatasetMetadataForDataset(
            [FromRoute] string fundingStreamId,
            [FromRoute] string datasetCode,
            [FromRoute] int versionNumber)
        {
            return await Task.FromResult(new OkObjectResult(Enumerable.Empty<Dataset>()));
        }
    }
}
