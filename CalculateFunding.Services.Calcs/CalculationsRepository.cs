using CalculateFunding.Models.Calcs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Calcs.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs
{
    public class CalculationsRepository : ICalculationsRepository
    {
        private readonly CosmosRepository _cosmosRepository;

        public CalculationsRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public Task<HttpStatusCode> CreateDraftCalculation(Calculation calculation)
        {
            return  _cosmosRepository.CreateAsync(calculation);
        }

        async public Task<Calculation> GetCalculationById(string calculationId)
        {
            var calculation = await _cosmosRepository.ReadAsync<Calculation>(calculationId);

            return calculation.Content;
        }
    }
}
