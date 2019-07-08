using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Api.External.V2.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using IPolicyFundingPeriodService = CalculateFunding.Services.Policy.Interfaces.IFundingPeriodService;

namespace CalculateFunding.Api.External.V2.Services
{
    public class TimePeriodsService : ITimePeriodsService
    {
        private readonly IPolicyFundingPeriodService _fundingPeriodService;
        private readonly IMapper _mapper;

        public TimePeriodsService(IPolicyFundingPeriodService fundingPeriodService, IMapper mapper)
        {
            Guard.ArgumentNotNull(fundingPeriodService, nameof(fundingPeriodService));
            Guard.ArgumentNotNull(mapper, nameof(mapper));

            _fundingPeriodService = fundingPeriodService;
            _mapper = mapper;
        }


        public async Task<IActionResult> GetFundingPeriods()
        {
            IActionResult actionResult = await _fundingPeriodService.GetFundingPeriods();

            if (actionResult is OkObjectResult okObjectResult)
            {
                IEnumerable<CalculateFunding.Models.Policy.Period> periods = (IEnumerable<CalculateFunding.Models.Policy.Period>)okObjectResult.Value;
                List<Models.Period> mappedPeriods = _mapper.Map<List<Models.Period>>(periods);
                return new OkObjectResult(mappedPeriods);
            }
            return actionResult;
        }
    }
}
