using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CalculateFunding.Api.External.Swagger.Helpers;
using CalculateFunding.Api.External.V2.Interfaces;
using CalculateFunding.Api.External.V2.Models;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.External.V2.Services
{
    public class AllocationsService : IAllocationsService
    {
        private readonly IPublishedResultsService _publishedResultsService;
        private readonly IFeatureToggle _featureToggle;

        public AllocationsService(IPublishedResultsService publishedResultsService, IFeatureToggle featureToggle)
        {
            Guard.ArgumentNotNull(publishedResultsService, nameof(publishedResultsService));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));

            _publishedResultsService = publishedResultsService;
            _featureToggle = featureToggle;
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

            return Formatter.ActionResult<AllocationModel>(httpRequest, allocation);
        }

        AllocationModel CreateAllocation(PublishedProviderResult publishedProviderResult)
        {
            ProviderVariation providerVariation = new ProviderVariation();

            PublishedAllocationLineResult allocationLineResult = publishedProviderResult.FundingStreamResult.AllocationLineResult;

            if (!allocationLineResult.Current.VariationReasons.IsNullOrEmpty())
            {
                providerVariation.VariationReasons = new Collection<string>(allocationLineResult.Current.VariationReasons.Select(vr => vr.ToString()).ToList());
            }

            if (allocationLineResult.Current.Provider.Successor != null)
            {
                providerVariation.Successors = new Collection<ProviderInformationModel> { new ProviderInformationModel() { Ukprn = allocationLineResult.Current.Provider.Successor } };
            }

            if (!allocationLineResult.Current.Predecessors.IsNullOrEmpty())
            {
                List<ProviderInformationModel> providerModelsForPredecessor = allocationLineResult.Current.Predecessors.Select(fi => new ProviderInformationModel() { Ukprn = fi }).ToList();
                providerVariation.Predecessors = new Collection<ProviderInformationModel>(providerModelsForPredecessor);
            }

            providerVariation.OpenReason = allocationLineResult.Current.Provider.ReasonEstablishmentOpened;
            providerVariation.CloseReason = allocationLineResult.Current.Provider.ReasonEstablishmentClosed;

            return new AllocationModel
            {
                AllocationResultTitle = $"Allocation {publishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.Name} was {publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Status}",
                AllocationResultId = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.FeedIndexId,
                AllocationAmount = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Value.HasValue ? (decimal)publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Value.Value : 0,
                AllocationMajorVersion = _featureToggle.IsAllocationLineMajorMinorVersioningEnabled() ? publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Major : 0,
                AllocationMinorVersion = _featureToggle.IsAllocationLineMajorMinorVersioningEnabled() ? publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Minor : 0,
                AllocationLine = new Models.AllocationLine
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
                Period = new Models.Period
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
                    Status = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.Status,
                    ProviderId = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.Id,
                    ProviderVariation = providerVariation

                },
                ProfilePeriods = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods != null ? new List<ProfilePeriod>(publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods.Select(m =>
                   new ProfilePeriod
                   {
                       DistributionPeriod = m.DistributionPeriod,
                       Occurrence = m.Occurrence,
                       Period = m.Period,
                       PeriodType = m.Type,
                       PeriodYear = m.Year.ToString(),
                       ProfileValue = (decimal)m.Value
                   }
                ).ToArraySafe()) : new List<ProfilePeriod>(),
            };
        }

        AllocationWithHistoryModel CreateAllocationWithHistoryModel(PublishedProviderResultWithHistory publishedProviderResultWithHistory)
        {
            AllocationWithHistoryModel allocationModel = new AllocationWithHistoryModel(CreateAllocation(publishedProviderResultWithHistory.PublishedProviderResult));

            allocationModel.History = new Collection<AllocationHistoryModel>(publishedProviderResultWithHistory.History?.Select(m =>
                   new AllocationHistoryModel
                   {
                       AllocationAmount = m.Value,
                       AllocationVersionNumber = m.Version,
                       Status = m.Status.ToString(),
                       Date = m.Date,
                       Author = m.Author.Name,
                       Comment = m.Comment,
                       AllocationMajorVersion = _featureToggle.IsAllocationLineMajorMinorVersioningEnabled() ? m.Major : 0,
                       AllocationMinorVersion = _featureToggle.IsAllocationLineMajorMinorVersioningEnabled() ? m.Minor : 0,
                   }
                ).OrderByDescending(m => m.Date).ToArraySafe());

            return allocationModel;
        }
    }
}
