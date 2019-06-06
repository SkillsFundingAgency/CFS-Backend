using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Models.CosmosDbScaling;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;

namespace CalculateFunding.Services.CosmosDbScaling
{
    public class CosmosDbScalingRequestModelBuilder : ICosmosDbScalingRequestModelBuilder
    {
        public CosmosDbScalingRequestModel BuildRequestModel(JobNotification jobNotification)
        {
            CosmosDbScalingRequestModel cosmosDbScalingRequestModel = new CosmosDbScalingRequestModel
            {
                JobDefinitionId = jobNotification.JobType
            };

            switch (jobNotification.JobType)
            {
                case JobConstants.DefinitionNames.CreateInstructAllocationJob:
                    cosmosDbScalingRequestModel.RepositoryTypes = new[]
                    {
                        CosmosRepositoryType.CalculationProviderResults,
                        CosmosRepositoryType.ProviderSourceDatasets
                    };
                    break;

                case JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob:
                    cosmosDbScalingRequestModel.RepositoryTypes = new[]
                    {
                        CosmosRepositoryType.ProviderSourceDatasets
                    };
                    break;

                case JobConstants.DefinitionNames.MapDatasetJob:
                    cosmosDbScalingRequestModel.RepositoryTypes = new[]
                    {
                        CosmosRepositoryType.ProviderSourceDatasets
                    };
                    break;

                case JobConstants.DefinitionNames.PublishProviderResultsJob:
                    cosmosDbScalingRequestModel.RepositoryTypes = new[]
                    {
                        CosmosRepositoryType.CalculationProviderResults,
                        CosmosRepositoryType.PublishedProviderResults
                    };
                    break;
            }

            return cosmosDbScalingRequestModel;
        }
    }
}
