using CalculateFunding.Models.CosmosDbScaling;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.CosmosDbScaling.Interfaces
{
    public interface ICosmosDbScalingConfigRepository
    {
        Task<CosmosDbScalingConfig> GetConfigByRepositoryType(CosmosRepositoryType cosmosRepositoryType);

        Task<IEnumerable<CosmosDbScalingConfig>> GetAllConfigs();

        Task<HttpStatusCode> UpdateCurrentRequestUnits(CosmosDbScalingConfig config);
    }
}
