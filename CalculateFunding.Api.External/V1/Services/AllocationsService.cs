using CalculateFunding.Api.External.Swagger.Helpers;
using CalculateFunding.Api.External.V1.Interfaces;
using CalculateFunding.Api.External.V1.Models;
using CalculateFunding.Models.External;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V1.Services
{
    public class AllocationsService : IAllocationsService
    {
        private readonly IResultsService _resultsService;

        public AllocationsService(IResultsService resultsService)
        {
            _resultsService = resultsService;
        }

        public async Task<IActionResult> GetAllocationByAllocationResultId(string allocationResultId, int? version, HttpRequest httpRequest)
        {
            Guard.IsNullOrWhiteSpace(allocationResultId, nameof(allocationResultId));
            Guard.ArgumentNotNull(httpRequest, nameof(httpRequest));

            if (version.HasValue && version < 1)
            {
                return new BadRequestObjectResult("Invalid version supplied");
            }

            PublishedProviderResult publishedProviderResult = await _resultsService.GetPublishedProviderResultByAllocationResultId(allocationResultId, version);

            if(publishedProviderResult == null)
            {
                return new NotFoundResult();
            }

            AllocationModel allocation = CreateAllocation(publishedProviderResult);

            return Formatter.ActionResult<AllocationModel>(httpRequest, allocation);
        }

        AllocationModel CreateAllocation(PublishedProviderResult publishedProviderResult)
        {
            return new AllocationModel
            {
                AllocationResultId = publishedProviderResult.Id,
                AllocationAmount = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Value.HasValue ? (decimal)publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Value.Value : 0,
                AllocationVersionNumber = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Version,
                AllocationLine = new AllocationLine
                {
                    AllocationLineCode = publishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.Id,
                    AllocationLineName = publishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.Name
                },
                AllocationStatus = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Status.ToString(),
                FundingStream = new AllocationFundingStreamModel
                {
                    FundingStreamCode = publishedProviderResult.FundingStreamResult.FundingStream.Id,
                    FundingStreamName = publishedProviderResult.FundingStreamResult.FundingStream.Name
                },
                Period = new Period
                {
                    PeriodId = publishedProviderResult.FundingPeriod.Id,
                    PeriodType = publishedProviderResult.FundingPeriod.Name,
                    StartDate = publishedProviderResult.FundingPeriod.StartDate,
                    EndDate = publishedProviderResult.FundingPeriod.EndDate
                },
                Provider = new AllocationProviderModel
                {
                    Ukprn = publishedProviderResult.Provider.UKPRN,
                    Upin = publishedProviderResult.Provider.UPIN,
                    ProviderOpenDate = publishedProviderResult.Provider.DateOpened
                }
            };
        }
    }
}
