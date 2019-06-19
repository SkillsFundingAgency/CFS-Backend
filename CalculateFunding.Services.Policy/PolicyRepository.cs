using System;
using System.Collections.Generic;
using System.Text;
using CalculateFunding.Common.CosmosDb;

namespace CalculateFunding.Services.Policy
{
    public class PolicyRepository : IPolicyRepository
    {
        private readonly ICosmosRepository _cosmosRepository;

        public PolicyRepository(ICosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }
    }
}
