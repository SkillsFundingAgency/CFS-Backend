using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Api.External.Swagger.Helpers;
using CalculateFunding.Api.External.V2.Interfaces;
using CalculateFunding.Api.External.V2.Models;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Models.External.AtomItems;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Models.Search;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CalculateFunding.Api.External.V2.Services
{
    public class AllocationNotificationFeedsService : IAllocationNotificationFeedsService
    {
        const int MaxRecords = 500;

        private readonly IAllocationNotificationsFeedsSearchService _feedsService;
        private readonly IFeatureToggle _featureToggle;

        public AllocationNotificationFeedsService(IAllocationNotificationsFeedsSearchService feedsService, IFeatureToggle featureToggle)
        {
            _feedsService = feedsService;
            _featureToggle = featureToggle;
        }

        public async Task<IActionResult> GetNotifications(HttpRequest request, int? pageRef, int? startYear = null, int? endYear = null, IEnumerable<string> fundingStreamIds = null, IEnumerable<string> allocationLineIds = null, IEnumerable<string> allocationStatuses = null, IEnumerable<string> ukprns = null, IEnumerable<string> laCodes = null, bool? isAllocationLineContractRequired = null, int? pageSize = MaxRecords)
        {
            pageSize = pageSize ?? MaxRecords;
            IEnumerable<string> statusesArray = allocationStatuses.IsNullOrEmpty() ? new[] { "Published" } : allocationStatuses;

            if (pageRef < 1)
            {
                return new BadRequestObjectResult("Page ref should be at least 1");
            }

            if (pageSize < 1 || pageSize > 500)
            {
                return new BadRequestObjectResult($"Page size should be more that zero and less than or equal to {MaxRecords}");
            }

            SearchFeedV2<AllocationNotificationFeedIndex> searchFeed = await _feedsService.GetFeedsV2(pageRef, pageSize.Value, startYear, endYear, ukprns, laCodes, isAllocationLineContractRequired, statusesArray, fundingStreamIds, allocationLineIds);

            if (searchFeed == null || searchFeed.TotalCount == 0 || searchFeed.Entries.IsNullOrEmpty())
            {
                return new NotFoundResult();
            }

            AtomFeed<AllocationModel> atomFeed = CreateAtomFeed(searchFeed, request);

            return Formatter.ActionResult<AtomFeed<AllocationModel>>(request, atomFeed);
        }

        private AtomFeed<AllocationModel> CreateAtomFeed(SearchFeedV2<AllocationNotificationFeedIndex> searchFeed, HttpRequest request)
        {
            const string notificationsEndpointName = "notifications";
            string baseRequestPath = request.Path.Value.Substring(0, request.Path.Value.IndexOf(notificationsEndpointName, StringComparison.Ordinal) + notificationsEndpointName.Length);
            string allocationTrimmedRequestPath = baseRequestPath.Replace(notificationsEndpointName, string.Empty).TrimEnd('/');

            string queryString = request.QueryString.Value;

            string notificationsUrl = $"{request.Scheme}://{request.Host.Value}{baseRequestPath}{{0}}{(!string.IsNullOrWhiteSpace(queryString) ? queryString : "")}";

            AtomFeed<AllocationModel> atomFeed = new AtomFeed<AllocationModel>
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = "Calculate Funding Service Allocation Feed",
                Author = new AtomAuthor
                {
                    Name = "Calculate Funding Service",
                    Email = "calculate-funding@education.gov.uk"
                },
                Updated = DateTimeOffset.Now,
                Rights = "Copyright (C) 2019 Department for Education",
                Link = searchFeed.GenerateAtomLinksForResultGivenBaseUrl(notificationsUrl).ToList(),
                AtomEntry = new List<AtomEntry<AllocationModel>>(),
                IsArchived = searchFeed.IsArchivePage

            };

            foreach (AllocationNotificationFeedIndex feedIndex in searchFeed.Entries)
            {
                ProviderVariation providerVariation = new ProviderVariation();

                if (!feedIndex.VariationReasons.IsNullOrEmpty())
                {
                    providerVariation.VariationReasons = new Collection<string>(feedIndex.VariationReasons);
                }

                if (!feedIndex.Successors.IsNullOrEmpty())
                {
                    List<ProviderInformationModel> providerInformationModels = feedIndex.Successors.Select(fi => new ProviderInformationModel() { Ukprn = fi }).ToList();
                    providerVariation.Successors = new Collection<ProviderInformationModel>(providerInformationModels);
                }

                if (!feedIndex.Predecessors.IsNullOrEmpty())
                {
                    List<ProviderInformationModel> providerInformationModels = feedIndex.Predecessors.Select(fi => new ProviderInformationModel() { Ukprn = fi }).ToList();
                    providerVariation.Predecessors = new Collection<ProviderInformationModel>(providerInformationModels);
                }

                providerVariation.OpenReason = feedIndex.OpenReason;
                providerVariation.CloseReason = feedIndex.CloseReason;

                atomFeed.AtomEntry.Add(new AtomEntry<AllocationModel>
                {
                    Id = $"{request.Scheme}://{request.Host.Value}{allocationTrimmedRequestPath}/{feedIndex.Id}",
                    Title = feedIndex.Title,
                    Summary = feedIndex.Summary,
                    Published = feedIndex.DatePublished,
                    Updated = feedIndex.DateUpdated.Value,
                    Version = $"{feedIndex.MajorVersion}.{feedIndex.MinorVersion}",
                    Link = new AtomLink("Allocation", $"{ request.Scheme }://{request.Host.Value}{allocationTrimmedRequestPath}/{feedIndex.Id}"),
                    Content = new AtomContent<AllocationModel>
                    {
                        Allocation = new AllocationModel
                        {
                            FundingStream = new AllocationFundingStreamModel
                            {
                                Id = feedIndex.FundingStreamId,
                                Name = feedIndex.FundingStreamName,
                                ShortName = feedIndex.FundingStreamShortName,
                                PeriodType = new AllocationFundingStreamPeriodTypeModel
                                {
                                    Id = feedIndex.FundingStreamPeriodId,
                                    Name = feedIndex.FundingStreamPeriodName,
                                    StartDay = feedIndex.FundingStreamStartDay,
                                    StartMonth = feedIndex.FundingStreamStartMonth,
                                    EndDay = feedIndex.FundingStreamEndDay,
                                    EndMonth = feedIndex.FundingStreamEndMonth
                                }
                            },
                            Period = new Period
                            {
                                Id = feedIndex.FundingPeriodId,
                                Name = feedIndex.FundingStreamPeriodName,
                                StartYear = feedIndex.FundingPeriodStartYear,
                                EndYear = feedIndex.FundingPeriodEndYear
                            },
                            Provider = new AllocationProviderModel
                            {
                                Name = feedIndex.ProviderName,
                                LegalName = feedIndex.ProviderLegalName,
                                UkPrn = feedIndex.ProviderUkPrn,
                                Upin = feedIndex.ProviderUpin,
                                Urn = feedIndex.ProviderUrn,
                                DfeEstablishmentNumber = feedIndex.DfeEstablishmentNumber,
                                EstablishmentNumber = feedIndex.EstablishmentNumber,
                                LaCode = feedIndex.LaCode,
                                LocalAuthority = feedIndex.Authority,
                                Type = feedIndex.ProviderType,
                                SubType = feedIndex.SubProviderType,
                                OpenDate = feedIndex.ProviderOpenDate,
                                CloseDate = feedIndex.ProviderClosedDate,
                                CrmAccountId = feedIndex.CrmAccountId,
                                NavVendorNo = feedIndex.NavVendorNo,
                                Status = feedIndex.ProviderStatus,
                                ProviderId = feedIndex.ProviderId,
                                ProviderVariation = providerVariation
                            },
                            AllocationLine = new AllocationLine
                            {
                                Id = feedIndex.AllocationLineId,
                                Name = feedIndex.AllocationLineName,
                                ShortName = feedIndex.AllocationLineShortName,
                                FundingRoute = feedIndex.AllocationLineFundingRoute,
                                ContractRequired = feedIndex.AllocationLineContractRequired ? "Y" : "N"
                            },
                            AllocationMajorVersion = feedIndex.MajorVersion ?? 0,
                            AllocationMinorVersion = feedIndex.MinorVersion ?? 0,
                            AllocationStatus = feedIndex.AllocationStatus,
                            AllocationAmount = (decimal)feedIndex.AllocationAmount,
                            AllocationResultId = feedIndex.Id,
                            AllocationResultTitle = feedIndex.Title,
                            ProfilePeriods = string.IsNullOrEmpty(feedIndex.ProviderProfiling)
                                ? new List<ProfilePeriod>()
                                : new List<ProfilePeriod>(JsonConvert
                                    .DeserializeObject<IEnumerable<ProfilingPeriod>>(feedIndex.ProviderProfiling)
                                    .Select(m => new ProfilePeriod(m.Period, m.Occurrence, m.Year.ToString(), m.Type, m.Value, m.DistributionPeriod))
                                    .ToArraySafe())
                        }
                    }
                });
            }

            return atomFeed;
        }

    }
}
