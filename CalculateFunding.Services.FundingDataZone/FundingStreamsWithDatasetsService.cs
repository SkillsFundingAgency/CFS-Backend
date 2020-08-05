using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.FundingDataZone.Interfaces;

namespace CalculateFunding.Services.FundingDataZone
{
    public class FundingStreamsWithDatasetsService : IFundingStreamsWithDatasetsService
    {
        private readonly IPublishingAreaRepository _publishingAreaRepository;

        public FundingStreamsWithDatasetsService(IPublishingAreaRepository publishingAreaRepository)
        {
            Guard.ArgumentNotNull(publishingAreaRepository, nameof(publishingAreaRepository));
            
            _publishingAreaRepository = publishingAreaRepository;
        }

        public async Task<IEnumerable<string>> GetFundingStreamsWithDatasets()
        {
            return await _publishingAreaRepository.GetFundingStreamsWithDatasets();
        }
    }
}
