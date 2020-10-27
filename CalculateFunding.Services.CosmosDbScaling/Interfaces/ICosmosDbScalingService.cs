using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.CosmosDbScaling;
using CalculateFunding.Services.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.CosmosDbScaling.Interfaces
{
    public interface ICosmosDbScalingService : IProcessingService
    {
        Task ScaleUp(IEnumerable<EventData> events);

        Task ScaleDownForJobConfiguration();

        Task ScaleDownIncrementally();

        Task<IActionResult> SaveConfiguration(ScalingConfigurationUpdateModel scalingConfigurationUpdate);
    }
}
