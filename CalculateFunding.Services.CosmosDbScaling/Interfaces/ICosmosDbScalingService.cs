﻿using Microsoft.Azure.EventHubs;
using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.CosmosDbScaling.Interfaces
{
    public interface ICosmosDbScalingService
    {
        Task ScaleUp(Message message);

        Task ScaleUp(IEnumerable<EventData> events);

        Task ScaleDownForJobConfiguration();

        Task ScaleDownIncrementally();
    }
}
