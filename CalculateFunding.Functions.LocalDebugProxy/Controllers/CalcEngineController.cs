using CalculateFunding.Services.Calculator;
using CalculateFunding.Services.Calculator.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.LocalDebugProxy.Controllers
{
    public class CalcEngineController : Controller
    {
        private IProviderSourceDatasetsRepository _providerSourceDatasetsRepository;
        private readonly ICalculationEngineService _calculationEngineService;

        public CalcEngineController(IProviderSourceDatasetsRepository providerSourceDatasetsRepository, ICalculationEngineService calculationEngineService)
        {
            _providerSourceDatasetsRepository = providerSourceDatasetsRepository;
            _calculationEngineService = calculationEngineService;
        }

        [Route("api/repotest")]
        [HttpGet]
        public async Task<IActionResult> RepoTest()
        {
            var result = await _providerSourceDatasetsRepository.GetProviderSourceDatasetsByProviderIdsAndSpecificationId(new[] { "108320", "106098" }, "bbc27ecb-aac0-43e0-ab6c-9a9b7b568065");
            return new OkObjectResult(result);
        }

        [Route("api/calctest")]
        [HttpPost]
        public async Task<IActionResult> CalcTest()
        {
            var result = await _calculationEngineService.GenerateAllocations(ControllerContext.HttpContext.Request);

            return new OkObjectResult(result);
        }
    }
}
