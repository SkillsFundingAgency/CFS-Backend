using CalculateFunding.Services.FDZ.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.FDZ
{
    public class FundingStreamsWithDatasetsService : IFundingStreamsWithDatasetsService
    {
        private readonly IPublishingAreaRepository _publishingAreaRepository;

        public FundingStreamsWithDatasetsService(IPublishingAreaRepository publishingAreaRepository)
        {
            _publishingAreaRepository = publishingAreaRepository;
        }

        public async Task<IEnumerable<string>> GetFundingStreamsWithDatasets()
        {
            return await _publishingAreaRepository.GetFundingStreamsWithDatasets();
        }
    }
}
