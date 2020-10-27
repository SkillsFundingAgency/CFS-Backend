using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Code;
using CalculateFunding.Services.Jobs.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ICodeContextCache : IJobProcessingService
    {
        Task<IActionResult> QueueCodeContextCacheUpdate(string specificationId);

        Task<IEnumerable<TypeInformation>> GetCodeContext(string specificationId);
    }
}