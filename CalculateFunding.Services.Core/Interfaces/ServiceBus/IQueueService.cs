using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Interfaces.ServiceBus
{
    public interface IQueueService
    {
        Task CreateQueue(string entityPath);

        Task DeleteQueue(string entityPath);
    }
}
