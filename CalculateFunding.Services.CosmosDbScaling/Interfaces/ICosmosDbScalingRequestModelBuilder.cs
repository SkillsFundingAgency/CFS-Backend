using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Models.CosmosDbScaling;
using System.Collections.Generic;

namespace CalculateFunding.Services.CosmosDbScaling.Interfaces
{
    public interface ICosmosDbScalingRequestModelBuilder
    {
        CosmosDbScalingRequestModel BuildRequestModel(IEnumerable<CosmosDbScalingConfig> cosmosDbScalingConfigs, JobSummary jobSummary);
    }
}
