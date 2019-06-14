using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using Microsoft.Azure.Documents;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.CosmosDbScaling.Repositories
{
    public abstract class CosmosDbScalingRepository : ICosmosDbScalingRepository
    {
        private readonly ICosmosRepository _cosmosRepository;

        public CosmosDbScalingRepository(ICosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public async Task SetThroughput(int throughput)
        {
            await _cosmosRepository.SetThroughput(throughput);
        }

        public async Task<int> GetCurrentThroughput()
        {
            return await _cosmosRepository.GetThroughput();
        }
    }
}
