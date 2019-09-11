using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.CosmosDbScaling;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.CosmosDbScaling.Interfaces
{
    public interface ICosmosDbScalingService
    {
        Task ScaleUp(Message message);

        Task ScaleUp(IEnumerable<EventData> events);

        Task ScaleDownForJobConfiguration();

        Task ScaleDownIncrementally();

        Task<IActionResult> SaveConfiguration(ScalingConfigurationUpdateModel scalingConfigurationUpdate);
    }
}
