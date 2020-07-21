using System.Threading.Tasks;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CalculateFunding.Api.FundingDataZone.Controllers
{
    [ApiController]
    public class DataDownloadController : ControllerBase
    {
        private readonly IDataDownloadService _dataDownloadService;

        public DataDownloadController(IDataDownloadService dataDownloadService)
        {
            _dataDownloadService = dataDownloadService;
        }

        private const string GetDataForDatasetVersionDescription = @"
Data is returned in the form of an array of objects, with the field names and values being at a single level
         eg 

```
[
    {
        id: 1,
        name: ""Row 1""
        ukprn: ""12345""
    },
    {
        id: 2,
        name: ""Row 2"",
        ukprn: ""23456""
    }
   ]
```


```
Given I am CFS user
And I have added a dataset attached to a dataset code
And I have mapped to a specific version of the dataset
When I run calculations for a specification
Then I am able to access the data for providers within the specification in that dataset
```
";

        /// <summary>Downloads data for a given dataset and version</summary>
        /// <param name="datasetCode">Dataset Code</param>
        /// <param name="versionNumber">Version of dataset</param>
        /// <returns></returns>
        [HttpGet("api/datasets/data/{datasetCode}/{versionNumber}")]
        [SwaggerOperation(
            Summary = "Downloads data for a given dataset and version", 
            Description = GetDataForDatasetVersionDescription)]
        [Produces(typeof(object))]
        public async Task<IActionResult> GetDataForDatasetVersion(
            [FromRoute] string datasetCode,
            [FromRoute]int versionNumber)
        {
            return await _dataDownloadService.GetDataForDataset(datasetCode, versionNumber);
        }
    }
}
