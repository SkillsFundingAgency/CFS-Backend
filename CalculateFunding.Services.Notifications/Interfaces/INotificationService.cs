﻿using System.Threading.Tasks;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;

namespace CalculateFunding.Services.Notifications.Interfaces
{
    public interface INotificationService : IProcessingService
    {
        Task OnNotificationEvent(Message message, IAsyncCollector<SignalRMessage> signalRMessages);
    }
}
