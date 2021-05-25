using System.Threading.Tasks;
using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IConverterDataMergeService : IJobProcessingService
    {
        Task<IActionResult> QueueJob(ConverterMergeRequest request);
        Task<IActionResult> GetConverterDataMergeLog(string id);
    }
}