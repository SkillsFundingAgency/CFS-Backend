using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public interface IReleasedSqlImportService : IJobProcessingService
    {
        Task<IActionResult> QueueSqlImport(string specificationId,
            string fundingStreamId,
            Reference user,
            string correlationId);
    }
}