using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Services.Core.Interfaces.Services;
using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Jobs.Interfaces
{
    public interface IJobProcessingService : IProcessingService
    {
        JobViewModel Job { get; }
    }
}
