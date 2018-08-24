using AutoMapper;
using CalculateFunding.Api.External.V1.Interfaces;
using CalculateFunding.Api.External.V1.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V1.Services
{
    public class FundingStreamService : IFundingStreamService
    {

        private readonly ISpecificationsService _specService;

        private readonly IMapper _mapper;

        public FundingStreamService(ISpecificationsService specService, IMapper mapper)
        {
            _specService = specService;
            _mapper = mapper;
        }

        public async Task<IActionResult> GetFundingStreams(HttpRequest request)
        {
            IActionResult result = await _specService.GetFundingStreams(request);

            if (result is OkObjectResult okObjectResult)
            {
                IEnumerable<CalculateFunding.Models.Specs.FundingStream> fundingStream = (IEnumerable<CalculateFunding.Models.Specs.FundingStream>)okObjectResult.Value;
                if (fundingStream.IsNullOrEmpty())
                {
                    return new InternalServerErrorResult("Could not find any funding streams");
                }
                else
                {
                    List<V1.Models.FundingStream> mappedFundingStreams = _mapper.Map<List<V1.Models.FundingStream>>(fundingStream);
                    return new OkObjectResult(mappedFundingStreams);
                }
            }
            return new OkObjectResult(result);
        }
    }
}
