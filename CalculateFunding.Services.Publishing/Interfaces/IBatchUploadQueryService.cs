using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IBatchUploadQueryService
    {
        Task<IActionResult> GetBatchProviderIds(string batchId);
    }
}