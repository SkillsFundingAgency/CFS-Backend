using CalculateFunding.Models.CosmosDbScaling;
using CalculateFunding.Services.CosmosDbScaling.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.CosmosDbScaling.Interfaces
{
    public interface ICosmosDbScalingRepositoryProvider
    {
        ICosmosDbScalingRepository GetRepository(CosmosCollectionType repositoryType);
    }
}
