using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Batches;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IBatchUploadService
    {
        Task<IActionResult> UploadBatch(BatchUploadRequest uploadRequest);
    }
}