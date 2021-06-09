using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Api.Datasets.Controllers
{
    public class SpecificationsController : ControllerBase
    {
        private readonly ISpecificationsService _specificationsService;

        public SpecificationsController(
            ISpecificationsService specificationsService)
        {
            Guard.ArgumentNotNull(specificationsService, nameof(specificationsService));

            _specificationsService = specificationsService;
        }

        [Route("api/specifications/{specificationId}/eligible-specification-references")]
        [HttpGet]
        [Produces(typeof(IEnumerable<EligibleSpecificationReference>))]
        public async Task<IActionResult> GetEligibleSpecificationsToReference(string specificationId)
            => await _specificationsService.GetEligibleSpecificationsToReference(specificationId);
    }
}
