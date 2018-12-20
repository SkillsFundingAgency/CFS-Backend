using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Results.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results
{
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
