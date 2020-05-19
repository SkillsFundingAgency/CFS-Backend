using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Results.Interfaces;

namespace CalculateFunding.Services.Results.Repositories
{
    public class CalculationsRepository : ICalculationsRepository
    {
        private readonly ICalculationsApiClient _calcsApiClient;

        public CalculationsRepository(ICalculationsApiClient calcsApiClient)
        {
            Guard.ArgumentNotNull(calcsApiClient, nameof(calcsApiClient));           

            _calcsApiClient = calcsApiClient;          
        }

        public async Task<Calculation> GetCalculationById(string calculationId)
        {
            Guard.ArgumentNotNull(calculationId, nameof(calculationId));

            ApiResponse<Calculation> apiResponse = await _calcsApiClient.GetCalculationById(calculationId);

            return apiResponse?.Content;
        }
    }
}
