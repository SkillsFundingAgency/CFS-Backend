using System;
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
        public ConverterDataMergeController(IConverterDataMergeService converterDataMergeService,
            ISpecificationConverterDataMerge specificationConverterDataMerge)
        {
            Guard.ArgumentNotNull(converterDataMergeService, nameof(converterDataMergeService));
            Guard.ArgumentNotNull(specificationConverterDataMerge, nameof(specificationConverterDataMerge));

            _converterDataMergeService = converterDataMergeService;
            _specificationConverterDataMerge = specificationConverterDataMerge;
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

        /// <summary>
        ///     Query ConverterDataMergeLog by id
        /// </summary>
        /// <param name="id">id of the log to query</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpGet("api/reports/converter-data-merge-log/{id}")]
        public async Task<IActionResult> GetConverterDataMergeLog([FromRoute] string id)
            => await _converterDataMergeService.GetConverterDataMergeLog(id);
    }
}