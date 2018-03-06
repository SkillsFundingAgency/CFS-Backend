using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IBuildProjectsService
    {
        Task GenerateAllocationsInstruction(Message message);
        Task UpdateAllocations(Message message);
    }
}
