using CalculateFunding.Common.CosmosDb;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.CosmosDbScaling.Repositories
{
    public class CalculationProviderResultsScalingRepository : CosmosDbScalingRepository
    {
        public CalculationProviderResultsScalingRepository(ICosmosRepository cosmosRepository) : base(cosmosRepository){}
    }
}
