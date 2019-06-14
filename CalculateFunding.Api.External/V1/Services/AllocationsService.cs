using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Api.External.Swagger.Helpers;
using CalculateFunding.Api.External.V1.Interfaces;
using CalculateFunding.Api.External.V1.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.External.V1.Services
{
    public class AllocationsService : IAllocationsService
    {
        private readonly IPublishedResultsService _publishedResultsService;

        public AllocationsService(IPublishedResultsService publishedResultsService)
        {
            Guard.ArgumentNotNull(publishedResultsService, nameof(publishedResultsService));

            _publishedResultsService = publishedResultsService;
        }

        public IActionResult GetAllocationByAllocationResultId(string allocationResultId, HttpRequest httpRequest)
        {
            Guard.IsNullOrWhiteSpace(allocationResultId, nameof(allocationResultId));
            Guard.ArgumentNotNull(httpRequest, nameof(httpRequest));

            PublishedProviderResult publishedProviderResult = _publishedResultsService.GetPublishedProviderResultByVersionId(allocationResultId);

            if (publishedProviderResult == null)
            {
                return new NotFoundResult();
            }

            AllocationModel allocation = CreateAllocation(publishedProviderResult);

            return Formatter.ActionResult(httpRequest, allocation);
        }

        public async Task<IActionResult> GetAllocationAndHistoryByAllocationResultId(string allocationResultId, HttpRequest httpRequest)
        {
            Guard.IsNullOrWhiteSpace(allocationResultId, nameof(allocationResultId));
            Guard.ArgumentNotNull(httpRequest, nameof(httpRequest));

            PublishedProviderResultWithHistory publishedProviderResultWithHistory = await _publishedResultsService.GetPublishedProviderResultWithHistoryByAllocationResultId(allocationResultId);

            if (publishedProviderResultWithHistory == null)
            {
                return new NotFoundResult();
            }

            AllocationWithHistoryModel allocation = CreateAllocationWithHistoryModel(publishedProviderResultWithHistory);

            return Formatter.ActionResult<AllocationModel>(httpRequest, allocation);
        }

        AllocationModel CreateAllocation(PublishedProviderResult publishedProviderResult)
        {
            return new AllocationModel
            {
                AllocationResultId = publishedProviderResult.Id,
                AllocationAmount = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Value ?? 0,
                AllocationVersionNumber = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Version,
                AllocationLine = new AllocationLine
                {
                    Id = publishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.Id,
                    Name = publishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.Name,
                    ShortName = publishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.ShortName,
                    FundingRoute = publishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.FundingRoute.ToString(),
                    ContractRequired = publishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.IsContractRequired ? "Y" : "N"
                },
                AllocationStatus = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Status.ToString(),
                FundingStream = new AllocationFundingStreamModel
                {
                    Id = publishedProviderResult.FundingStreamResult.FundingStream.Id,
                    Name = publishedProviderResult.FundingStreamResult.FundingStream.Name,
                    ShortName = publishedProviderResult.FundingStreamResult.FundingStream.ShortName,
                    PeriodType = new AllocationFundingStreamPeriodTypeModel
                    {
                        Id = publishedProviderResult.FundingStreamResult.FundingStream.PeriodType.Id,
                        Name = publishedProviderResult.FundingStreamResult.FundingStream.PeriodType.Id,
                        StartDay = publishedProviderResult.FundingStreamResult.FundingStream.PeriodType.StartDay,
                        StartMonth = publishedProviderResult.FundingStreamResult.FundingStream.PeriodType.StartMonth,
                        EndDay = publishedProviderResult.FundingStreamResult.FundingStream.PeriodType.EndDay,
                        EndMonth = publishedProviderResult.FundingStreamResult.FundingStream.PeriodType.EndMonth,
                    }
                },
                Period = new Period
                {
                    Id = publishedProviderResult.FundingPeriod.Id,
                    Name = publishedProviderResult.FundingPeriod.Name,
                    StartYear = publishedProviderResult.FundingPeriod.StartYear,
                    EndYear = publishedProviderResult.FundingPeriod.EndYear
                },
                Provider = new AllocationProviderModel
                {
                    Name = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.Name,
                    LegalName = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.LegalName,
                    UkPrn = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.UKPRN,
                    Upin = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.UPIN,
                    Urn = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.URN,
                    DfeEstablishmentNumber = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.DfeEstablishmentNumber,
                    EstablishmentNumber = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.EstablishmentNumber,
                    LaCode = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.LACode,
                    LocalAuthority = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.Authority,
                    Type = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.ProviderType,
                    SubType = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.ProviderSubType,
                    OpenDate = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.DateOpened,
                    CloseDate = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.DateClosed,
                    CrmAccountId = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.CrmAccountId,
                    NavVendorNo = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.NavVendorNo,
                    Status = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.Status

                },
                ProfilePeriods = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods?.Select(m =>
                            new ProfilePeriod
                            {
                                DistributionPeriod = m.DistributionPeriod,
                                Occurrence = m.Occurrence,
                                Period = m.Period,
                                PeriodType = m.Type,
                                PeriodYear = m.Year.ToString(),
                                ProfileValue = m.Value
                            }
                    ).ToArraySafe()
            };
        }

        AllocationWithHistoryModel CreateAllocationWithHistoryModel(PublishedProviderResultWithHistory publishedProviderResultWithHistory)
        {
            AllocationWithHistoryModel allocationModel = new AllocationWithHistoryModel(CreateAllocation(publishedProviderResultWithHistory.PublishedProviderResult));

            allocationModel.History = publishedProviderResultWithHistory.History?.Select(m =>
                   new AllocationHistoryModel
                   {
                       AllocationAmount = m.Value,
                       AllocationVersionNumber = m.Version,
                       AllocationVersion = decimal.Parse(m.VersionNumber),
                       Status = m.Status.ToString(),
                       Date = m.Date,
                       Author = m.Author.Name,
                       Comment = m.Comment
                   }
                ).OrderByDescending(m => m.Date).ToArraySafe();

            return allocationModel;
        }
    }
}
