using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.FundingDataZone
{
    public class DataDownloadService : IDataDownloadService
    {
        private readonly IPublishingAreaRepository _publishingAreaRepository;

        public DataDownloadService(IPublishingAreaRepository publishingAreaRepository)
        {
            Guard.ArgumentNotNull(publishingAreaRepository, nameof(publishingAreaRepository));
            
            _publishingAreaRepository = publishingAreaRepository;
        }

        public async Task<IActionResult> GetDataForDataset(string datasetCode, int version)
        {
            Guard.IsNullOrWhiteSpace(datasetCode, nameof(datasetCode));
            
            string tableName = await _publishingAreaRepository.GetTableNameForDataset(datasetCode, version);

            if (tableName.IsNullOrWhitespace())
            {
                return new NotFoundObjectResult(new ProblemDetails()
                {
                    Title = "Dataset not found",
                    Status = 404
                });
            }

            object datasetResults = await _publishingAreaRepository.GetDataForTable(tableName);

            return new OkObjectResult(datasetResults);
        }
    }
}
