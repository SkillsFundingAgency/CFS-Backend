using Microsoft.Azure.ServiceBus;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Processing.Interfaces
{
    public interface IProcessingService
    {
        Task Run(Message message, Func<Task> func = null);

        Task Process(Message message);
    }
}
