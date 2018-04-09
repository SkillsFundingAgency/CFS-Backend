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

        public CalcEngineController(IProviderSourceDatasetsRepository providerSourceDatasetsRepository)
        {
            _providerSourceDatasetsRepository = providerSourceDatasetsRepository;
        }

        [Route("api/repotest")]
        [HttpGet]
        public async Task<IActionResult> RepoTest()
        {
            var result = await _providerSourceDatasetsRepository.GetProviderSourceDatasetsByProviderIdsAndSpecificationId(new[] { "108320", "106098" }, "bbc27ecb-aac0-43e0-ab6c-9a9b7b568065");
            return new OkObjectResult(result);
        }
    }
}
