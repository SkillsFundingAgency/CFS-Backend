using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Results.Interfaces;

namespace CalculateFunding.Services.Results.Repositories
{
    public class CalculationsRepository : ICalculationsRepository
    {
        private readonly IMapper _mapper;
        private readonly ICalculationsApiClient _calcsApiClient;

        public CalculationsRepository(ICalculationsApiClient calcsApiClient, IMapper mapper)
        {
            Guard.ArgumentNotNull(calcsApiClient, nameof(calcsApiClient));
            Guard.ArgumentNotNull(mapper, nameof(mapper));

            _calcsApiClient = calcsApiClient;
            _mapper = mapper;
        }

        public async Task<Calculation> GetCalculationById(string calculationId)
        {
            Guard.ArgumentNotNull(calculationId, nameof(calculationId));

            ApiResponse<Common.ApiClient.Calcs.Models.Calculation> apiResponse = await _calcsApiClient.GetCalculationById(calculationId);

            return _mapper.Map<Calculation>(apiResponse?.Content);
        }
    }
}
