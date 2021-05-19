using System.Threading.Tasks;
using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface ISpecificationConverterDataMerge : IProcessingService
    {
        Task<IActionResult> QueueJob(SpecificationConverterMergeRequest request);
    }
}