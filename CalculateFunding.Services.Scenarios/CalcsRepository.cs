using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Scenarios.Interfaces;

namespace CalculateFunding.Services.Scenarios
{
    public class CalcsRepository : ICalcsRepository
    {
        private readonly IMapper _mapper;
        private readonly ICalculationsApiClient _apiClient;

        public CalcsRepository(ICalculationsApiClient apiClient, IMapper mapper)
        {
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));
            Guard.ArgumentNotNull(mapper, nameof(mapper));

            _apiClient = apiClient;
            _mapper = mapper;
        }

        public async Task<IEnumerable<Calculation>> GetCurrentCalculationsBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                throw new ArgumentNullException(nameof(specificationId));
            }

            ApiResponse<IEnumerable<Calculation>> apiResponse = await _apiClient.GetCalculationsForSpecification(specificationId);

            return apiResponse?.Content;
        }
    }
}
