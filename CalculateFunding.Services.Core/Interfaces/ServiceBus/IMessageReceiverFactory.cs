using System;
using System.Collections.Generic;
using System.Text;
using AzureCore = Microsoft.Azure.ServiceBus.Core;

namespace CalculateFunding.Services.Core.Interfaces.ServiceBus
{
    public interface IMessageReceiverFactory
    {
        AzureCore.IMessageReceiver Receiver(string queueName);
        void TimedOut();
    }
}
