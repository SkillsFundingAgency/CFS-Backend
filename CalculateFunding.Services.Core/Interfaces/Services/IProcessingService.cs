using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Interfaces.Services
{
    public interface IProcessingService
    {
        Task Run(Message message, Func<Task> func = null);

        Task Process(Message message);
    }
}
