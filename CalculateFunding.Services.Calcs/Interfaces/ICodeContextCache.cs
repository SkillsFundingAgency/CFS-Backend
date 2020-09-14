using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Code;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ICodeContextCache
    {
        Task<IActionResult> QueueCodeContextCacheUpdate(string specificationId);
        
        Task UpdateCodeContextCacheEntry(Message message);
        Task<IEnumerable<TypeInformation>> GetCodeContext(string specificationId);
    }
}