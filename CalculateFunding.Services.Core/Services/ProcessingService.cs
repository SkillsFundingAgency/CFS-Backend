using CalculateFunding.Services.Core.Interfaces.Services;
using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Services
{
    public abstract class ProcessingService : IProcessingService
    {
        public virtual async Task Run(Message message, Func<Task> func = null)
        {
            if (func != null)
            {
                await func();
            }
            else
            {
                await Process(message);
            }
        }

        public abstract Task Process(Message message);
    }
}
