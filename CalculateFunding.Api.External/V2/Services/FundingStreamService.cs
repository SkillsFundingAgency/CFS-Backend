using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Api.External.V2.Interfaces;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.External.V2.Services
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
                    List<V2.Models.FundingStream> mappedFundingStreams = _mapper.Map<List<V2.Models.FundingStream>>(fundingStream);
                    return new OkObjectResult(mappedFundingStreams);
                }
            }
            return result;
        }

        public async Task<IActionResult> GetFundingStream(string fundingStreamId)
        {
            IActionResult result = await _fundingService.GetFundingStreamById(fundingStreamId);

            if (result is OkObjectResult okObjectResult)
            {
                V2.Models.FundingStream mappedFundingStream = _mapper.Map<V2.Models.FundingStream>(okObjectResult.Value);
                return new OkObjectResult(mappedFundingStream);
            }
            else
            {
                return result;
            }
        }
    }
}
