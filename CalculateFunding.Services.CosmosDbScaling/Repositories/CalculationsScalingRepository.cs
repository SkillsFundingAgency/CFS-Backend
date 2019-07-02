using CalculateFunding.Common.CosmosDb;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.CosmosDbScaling.Repositories
{

    public class CalculationsScalingRepository : CosmosDbScalingRepository
    {
        public CalculationsScalingRepository(ICosmosRepository cosmosRepository) : base(cosmosRepository) { }
    }
}
