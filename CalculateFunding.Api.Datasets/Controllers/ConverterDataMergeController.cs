using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Datasets.Controllers
{
    /// <summary>
    ///     Converter data merge end points
    /// </summary>
    [ApiController]
    public class ConverterDataMergeController : ControllerBase
    {
        private readonly IConverterDataMergeService _converterDataMergeService;
        private readonly ISpecificationConverterDataMerge _specificationConverterDataMerge;

        /// <inheritdoc />
        public ConverterDataMergeController(IConverterDataMergeService converterDataMergeService)
        {
            Guard.ArgumentNotNull(converterDataMergeService, nameof(converterDataMergeService));

            _converterDataMergeService = converterDataMergeService;
        }

        /// <summary>
        ///     Queues job to run converter data merge per supplied request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("api/datasets/converter/merge")]
        public async Task<IActionResult> QueueJob([FromBody] ConverterMergeRequest request)
            => await _converterDataMergeService.QueueJob(request);
        
        /// <summary>
        ///     Queues job to run converter data merge per supplied request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("api/specifications/datasets/converter/merge")]
        public async Task<IActionResult> QueueSpecificationParentJob([FromBody] SpecificationConverterMergeRequest request)
            => await _specificationConverterDataMerge.QueueJob(request);
    }
}