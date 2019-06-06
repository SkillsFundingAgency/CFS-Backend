using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Models.CosmosDbScaling;

namespace CalculateFunding.Services.CosmosDbScaling.Interfaces
{
    public interface ICosmosDbScalingRequestModelBuilder
    {
        CosmosDbScalingRequestModel BuildRequestModel(JobNotification jobNotification);
    }
}
