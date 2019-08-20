using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Api.External.V2.Interfaces;
using CalculateFunding.Common.Utility;
using Microsoft.AspNetCore.Mvc;
using IPolicyFundingStreamService = CalculateFunding.Services.Policy.Interfaces.IFundingStreamService;

namespace CalculateFunding.Api.External.V2.Services
{
    public class FundingStreamService : IFundingStreamService
    {
        private readonly IPolicyFundingStreamService _fundingStreamService;
        private readonly IMapper _mapper;

        public FundingStreamService(IPolicyFundingStreamService fundingStreamService, IMapper mapper)
        {
            Guard.ArgumentNotNull(fundingStreamService, nameof(fundingStreamService));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            _fundingStreamService = fundingStreamService;
            _mapper = mapper;
        }

        public async Task<IActionResult> GetFundingStreams()
        {
            IActionResult result = await _fundingStreamService.GetFundingStreams();

            if (result is OkObjectResult okObjectResult)
            {
                IEnumerable<CalculateFunding.Models.Obsoleted.FundingStream> fundingStream = (IEnumerable<CalculateFunding.Models.Obsoleted.FundingStream>)okObjectResult.Value;
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
            IActionResult result = await _fundingStreamService.GetFundingStreamById(fundingStreamId);

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
