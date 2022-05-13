using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Models.CosmosDbScaling;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using System.Collections.Generic;
using System.Linq;
using JobDefinitions = CalculateFunding.Services.Core.Constants.JobConstants.DefinitionNames;

namespace CalculateFunding.Services.CosmosDbScaling
{
    public class CosmosDbScalingRequestModelBuilder : ICosmosDbScalingRequestModelBuilder
    {
        public CosmosDbScalingRequestModel BuildRequestModel(IEnumerable<CosmosDbScalingConfig> cosmosDbScalingConfigs, JobSummary jobSummary)
        {
            CosmosDbScalingRequestModel cosmosDbScalingRequestModel = new CosmosDbScalingRequestModel
            {
                JobDefinitionId = jobSummary.JobType
            };

            cosmosDbScalingRequestModel.RepositoryTypes = cosmosDbScalingConfigs.Where(_ => _.JobRequestUnitConfigs.Any(ru => ru.JobDefinitionId.Equals(jobSummary.JobType)))
                .Select(_ => _.RepositoryType);

            cosmosDbScalingRequestModel.RepositoryTypes = cosmosDbScalingRequestModel.RepositoryTypes.Any() ?
                cosmosDbScalingRequestModel.RepositoryTypes :
                null;

            return cosmosDbScalingRequestModel;
        }
    }
}
