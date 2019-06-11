using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Api.External.V1.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.External.V1.Services
{
    public class FundingStreamService : IFundingStreamService
    {
        private readonly IFundingService _fundingService;
        private readonly IMapper _mapper;

        public FundingStreamService(IFundingService fundingService, IMapper mapper)
        {
            Guard.ArgumentNotNull(fundingService, nameof(fundingService));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            _fundingService = fundingService;
            _mapper = mapper;
        }

        public async Task<IActionResult> GetFundingStreams()
        {
            IActionResult result = await _fundingService.GetFundingStreams();

            if (result is OkObjectResult okObjectResult)
            {
                IEnumerable<CalculateFunding.Models.Specs.FundingStream> fundingStream = (IEnumerable<CalculateFunding.Models.Specs.FundingStream>)okObjectResult.Value;
                if (fundingStream.IsNullOrEmpty())
                {
                    return new OkResult();
                }
                else
                {
                    List<V1.Models.FundingStream> mappedFundingStreams = _mapper.Map<List<V1.Models.FundingStream>>(fundingStream);
                    return new OkObjectResult(mappedFundingStreams);
                }
            }
            return result;
        }
    }
}
