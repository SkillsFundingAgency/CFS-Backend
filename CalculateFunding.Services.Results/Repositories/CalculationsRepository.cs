using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Results.Interfaces;

namespace CalculateFunding.Services.Results.Repositories
{
    [Obsolete("Replace with common nuget API client")]
    public class CalculationsRepository : ICalculationsRepository
    {
        private const string calcsUrl = "calcs/calculation-by-id?calculationId=";
        private readonly ICalcsApiClientProxy _calcsApiClient;

        public CalculationsRepository(ICalcsApiClientProxy calcsApiClient)
        {
            _calcsApiClient = calcsApiClient;
        }

        public async Task<Calculation> GetCalculationById(string calculationId)
        {
            Guard.ArgumentNotNull(calculationId, nameof(calculationId));

            string url = $"{calcsUrl}{calculationId}";

            return await _calcsApiClient.GetAsync<Calculation>(url);
        }
    }
}
