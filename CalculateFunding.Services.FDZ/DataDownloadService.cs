using System.Threading.Tasks;
using CalculateFunding.Services.FDZ.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.FDZ
{
    public class DataDownloadService : IDataDownloadService
    {
        private readonly IPublishingAreaRepository _publishingAreaRepository;

        public DataDownloadService(IPublishingAreaRepository publishingAreaRepository)
        {
            _publishingAreaRepository = publishingAreaRepository;
        }

        public async Task<IActionResult> GetDataForDataset(string datasetCode, int version)
        {
            string tableName = await _publishingAreaRepository.GetTableNameForDataset(datasetCode, version);

            if (string.IsNullOrWhiteSpace(tableName))
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
