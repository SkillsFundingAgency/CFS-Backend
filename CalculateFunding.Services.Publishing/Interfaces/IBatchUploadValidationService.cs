using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Processing.Interfaces;
using CalculateFunding.Services.Publishing.Batches;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IBatchUploadValidationService : IJobProcessingService
    {
        Task<IActionResult> QueueBatchUploadValidation(BatchUploadValidationRequest batchUploadValidationRequest,
            Reference user,
            string correlationId);
    }
}