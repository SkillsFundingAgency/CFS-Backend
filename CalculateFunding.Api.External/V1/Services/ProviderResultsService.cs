using CalculateFunding.Api.External.Swagger.Helpers;
using CalculateFunding.Api.External.V1.Interfaces;
using CalculateFunding.Api.External.V1.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Search;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace CalculateFunding.Api.External.V1.Services
{
    public class ProviderResultsService : IProviderResultsService
    {
        private readonly IAllocationNotificationsFeedsSearchService _feedsService;
        private readonly ILogger _logger;

        public ProviderResultsService(IAllocationNotificationsFeedsSearchService feedsService, ILogger logger)
        {
            _feedsService = feedsService;
            _logger = logger;
        }

        public async Task<IActionResult> GetProviderResultsForAllocations(string providerId, int startYear, int endYear, string allocationLineIds, HttpRequest request)
        {
            if (string.IsNullOrEmpty(providerId))
            {
                return new BadRequestObjectResult("Missing providerId");
            }

            if (startYear == 0)
            {
                return new BadRequestObjectResult("Missing start year");
            }

            if (endYear == 0)
            {
                return new BadRequestObjectResult("Missing end year");
            }

            if (string.IsNullOrEmpty(allocationLineIds))
            {
                return new BadRequestObjectResult("Missing allocation line ids");
            }

            string[] allocationLineIdsArray = new string[0];

            if (!string.IsNullOrWhiteSpace(allocationLineIds))
            {
                allocationLineIdsArray = allocationLineIds.Split(",");
            }

            IList<string> allocationLineFilters = new List<string>();

            foreach (string allocationLineId in allocationLineIdsArray)
            {
                allocationLineFilters.Add($"allocationLineId eq '{allocationLineId}'");
            }

            SearchFeed<AllocationNotificationFeedIndex> searchFeed = await _feedsService.GetFeeds(providerId, startYear, endYear, allocationLineFilters);

            if (searchFeed == null || searchFeed.Entries.IsNullOrEmpty())
            {
                return new NotFoundResult();
            }

            IEnumerable<AllocationNotificationFeedIndex> entries = ValidateFeeds(searchFeed.Entries, checkProfiling: false);

            if (entries.IsNullOrEmpty())
            {
                return new NotFoundResult();
            }

            ProviderResultSummary providerResultSummary = CreateProviderResultSummary(entries, request);

            return Formatter.ActionResult(request, providerResultSummary);
        }

        public async Task<IActionResult> GetProviderResultsForFundingStreams(string providerId, int startYear, int endYear, string fundingStreamIds, HttpRequest request)
        {
            if (string.IsNullOrEmpty(providerId))
            {
                return new BadRequestObjectResult("Missing providerId");
            }

            if (startYear == 0)
            {
                return new BadRequestObjectResult("Missing start year");
            }

            if (endYear == 0)
            {
                return new BadRequestObjectResult("Missing end year");
            }

            if (string.IsNullOrEmpty(fundingStreamIds))
            {
                return new BadRequestObjectResult("Missing funding stream ids");
            }

            string[] fundingStreamIdsArray = new string[0];

            if (!string.IsNullOrWhiteSpace(fundingStreamIds))
            {
                fundingStreamIdsArray = fundingStreamIds.Split(",");
            }

            IList<string> fundingStreamFilters = new List<string>();

            foreach (string fundingStreamId in fundingStreamIdsArray)
            {
                fundingStreamFilters.Add($"fundingStreamId eq '{fundingStreamId}'");
            }

            SearchFeed<AllocationNotificationFeedIndex> searchFeed = await _feedsService.GetFeeds(providerId, startYear, endYear, fundingStreamFilters);

            if (searchFeed == null || searchFeed.Entries.IsNullOrEmpty())
            {
                return new NotFoundResult();
            }

            IEnumerable<AllocationNotificationFeedIndex> entries = ValidateFeeds(searchFeed.Entries, checkProfiling: false);

            if (entries.IsNullOrEmpty())
            {
                return new NotFoundResult();
            }

            ProviderResultSummary providerResultSummary = CreateProviderResultSummary(entries, request);

            return Formatter.ActionResult(request, providerResultSummary);
        }

        public async Task<IActionResult> GetLocalAuthorityProvidersResultsForAllocations(string laCode, int startYear, int endYear, string allocationLineIds, HttpRequest request)
        {
            if (string.IsNullOrEmpty(laCode))
            {
                return new BadRequestObjectResult("Missing la code");
            }

            if (startYear == 0)
            {
                return new BadRequestObjectResult("Missing start year");
            }

            if (endYear == 0)
            {
                return new BadRequestObjectResult("Missing end year");
            }

            if (string.IsNullOrEmpty(allocationLineIds))
            {
                return new BadRequestObjectResult("Missing allocation line ids");
            }

            string[] allocationLineIdsArray = new string[0];

            if (!string.IsNullOrWhiteSpace(allocationLineIds))
            {
                allocationLineIdsArray = allocationLineIds.Split(",");
            }

            IList<string> allocationLineFilters = new List<string>();

            foreach (string allocationLineId in allocationLineIdsArray)
            {
                allocationLineFilters.Add($"allocationLineId eq '{allocationLineId}'");
            }

            SearchFeed<AllocationNotificationFeedIndex> searchFeed = await _feedsService.GetLocalAuthorityFeeds(laCode, startYear, endYear, allocationLineFilters);

            if (searchFeed == null || searchFeed.Entries.IsNullOrEmpty())
            {
                return new NotFoundResult();
            }

            IEnumerable<AllocationNotificationFeedIndex> entries = ValidateFeedsForLaCode(searchFeed.Entries);

            if (entries.IsNullOrEmpty())
            {
                return new NotFoundResult();
            }

            LocalAuthorityResultsSummary localAuthorityResultsSummary = CreateLocalAuthorityResultsSummary(entries, request);

            return Formatter.ActionResult(request, localAuthorityResultsSummary);
        }

        private LocalAuthorityResultsSummary CreateLocalAuthorityResultsSummary(IEnumerable<AllocationNotificationFeedIndex> entries, HttpRequest request)
        {
            IEnumerable<IGrouping<string, AllocationNotificationFeedIndex>> localAuthorityResultSummaryGroups = entries.GroupBy(m => m.LaCode);

            AllocationNotificationFeedIndex firstEntry = entries.First();

            LocalAuthorityResultsSummary localAuthorityResultsSummary = new LocalAuthorityResultsSummary
            {
                FundingPeriod = $"{firstEntry.FundingPeriodStartYear}-{firstEntry.FundingPeriodEndYear}"
            };

            foreach (IGrouping<string, AllocationNotificationFeedIndex> localAuthorityResultSummaryGroup in localAuthorityResultSummaryGroups)
            {
                AllocationNotificationFeedIndex firstFeedIndex = localAuthorityResultSummaryGroup.First();

                LocalAuthorityResultSummary localAuthorityResultSummary = new LocalAuthorityResultSummary
                {
                    LANo = firstFeedIndex.LaCode,
                    LAName = firstFeedIndex.Authority
                };

                foreach (AllocationNotificationFeedIndex feedIndex in localAuthorityResultSummaryGroup)
                {
                    LocalAuthorityProviderResultSummary resultSummary = new LocalAuthorityProviderResultSummary
                    {
                        //temp until we sort out ukprns
                        Ukprn = string.IsNullOrWhiteSpace(feedIndex.ProviderUkPrn) ? feedIndex.ProviderId : feedIndex.ProviderUkPrn ,
                        LAEStab = feedIndex.EstablishmentNumber,
                        OrganisationName = feedIndex.ProviderName,
                        OrganisationType = feedIndex.ProviderType,
                        OrganisationSubType = feedIndex.SubProviderType,
                        EligiblePupils = feedIndex.AllocationLearnerCount
                    };

                    IEnumerable<IGrouping<string, AllocationNotificationFeedIndex>> fundingPeriodResultSummaryGroups = localAuthorityResultSummaryGroup.GroupBy(m => m.FundingPeriodId);

                    foreach (IGrouping<string, AllocationNotificationFeedIndex> fundingPeriodResultSummaryGroup in fundingPeriodResultSummaryGroups)
                    {
                        AllocationNotificationFeedIndex fundingPeriodFeedIndex = fundingPeriodResultSummaryGroup.First();

                        FundingPeriodResultSummary fundingPeriodResultSummary = new FundingPeriodResultSummary
                        {
                            FundingPeriod = new Period
                            {
                                PeriodId = fundingPeriodFeedIndex.FundingPeriodId,
                                StartDate = fundingPeriodFeedIndex.FundingPeriodStartDate,
                                EndDate = fundingPeriodFeedIndex.FundingPeriodEndDate,
                                PeriodType = fundingPeriodFeedIndex.FundingPeriodType
                            }
                        };

                        foreach (AllocationNotificationFeedIndex allocationFeedIndex in fundingPeriodResultSummaryGroup)
                        {
                            if (allocationFeedIndex.ProviderId != resultSummary.Ukprn)
                            {
                                continue;
                            }

                            AllocationResultWIthProfilePeriod allocationResultWIthProfilePeriod = new AllocationResultWIthProfilePeriod
                            {
                                AllocationLine = new AllocationLine
                                {
                                    AllocationLineCode = allocationFeedIndex.AllocationLineId,
                                    AllocationLineName = allocationFeedIndex.AllocationLineName
                                },
                                AllocationStatus = allocationFeedIndex.AllocationStatus,
                                AllocationVersionNumber = (ushort)allocationFeedIndex.AllocationVersionNumber,
                                AllocationAmount = Convert.ToDecimal(allocationFeedIndex.AllocationAmount)
                            };

                            resultSummary.AllocationValue += allocationResultWIthProfilePeriod.AllocationAmount;

                            ProfilingPeriod[] profilingPeriods = JsonConvert.DeserializeObject<ProfilingPeriod[]>(allocationFeedIndex.ProviderProfiling);

                            foreach (ProfilingPeriod profilingPeriod in profilingPeriods)
                            {
                                allocationResultWIthProfilePeriod.ProfilePeriods = allocationResultWIthProfilePeriod.ProfilePeriods.Concat(new[]
                                {
                                    new ProfilePeriod
                                    {
                                        Period = profilingPeriod.Period,
                                        Occurrence = profilingPeriod.Occurrence,
                                        PeriodYear = profilingPeriod.Year.ToString(),
                                        PeriodType = profilingPeriod.Type,
                                        ProfileValue = profilingPeriod.Value,
                                        DistributionPeriod = profilingPeriod.DistributionPeriod
                                    }
                                }).ToArraySafe();
                            }

                            fundingPeriodResultSummary.Allocations = fundingPeriodResultSummary.Allocations.Concat(new[]
                            {
                               allocationResultWIthProfilePeriod

                            }).ToArraySafe();
                        }

                        resultSummary.FundingPeriods = resultSummary.FundingPeriods.Concat(new[]
                        {
                            fundingPeriodResultSummary
                        }).ToArraySafe();
                    }

                    localAuthorityResultSummary.Providers = localAuthorityResultSummary.Providers.Concat(new[] { resultSummary }).ToArraySafe();
                }

                localAuthorityResultSummary.TotalAllocation = localAuthorityResultSummary.Providers.Sum(m => m.AllocationValue);

                localAuthorityResultsSummary.LocalAuthorities = localAuthorityResultsSummary.LocalAuthorities.Concat(new[] { localAuthorityResultSummary }).ToArraySafe();
            }

            localAuthorityResultsSummary.TotalAllocation = localAuthorityResultsSummary.LocalAuthorities.Sum(m => m.TotalAllocation);

            return localAuthorityResultsSummary;
        }

        private ProviderResultSummary CreateProviderResultSummary(IEnumerable<AllocationNotificationFeedIndex> entries, HttpRequest request)
        {
            IEnumerable<IGrouping<string, AllocationNotificationFeedIndex>> fundingPeriodResultSummaryGroups = entries.GroupBy(m => m.FundingPeriodId);

            AllocationNotificationFeedIndex firstEntry = entries.First();

            ProviderResultSummary providerResutSummary = new ProviderResultSummary
            {
                Provider = new Provider
                {
                    //AB: temporary why we dont have ukprns
                    Ukprn = !string.IsNullOrWhiteSpace(firstEntry.ProviderUkPrn) ? firstEntry.ProviderUkPrn : firstEntry.ProviderId,
                    ProviderOpenDate = firstEntry.ProviderOpenDate,
                    LegalName = firstEntry.ProviderName?.ToUpper(),
                    LAEstablishmentNo = firstEntry.EstablishmentNumber,
                    LANo = firstEntry.LaCode
                }
            };

            foreach(IGrouping<string, AllocationNotificationFeedIndex> fundingPeriodResultSummaryGroup in fundingPeriodResultSummaryGroups)
            {
                AllocationNotificationFeedIndex firstPeriodEntry = fundingPeriodResultSummaryGroup.First();

                ProviderPeriodResultSummary providerPeriodResultSummary = new ProviderPeriodResultSummary
                {
                    Period = new Period
                    {
                        PeriodType = firstPeriodEntry.FundingPeriodType,
                        PeriodId = firstPeriodEntry.FundingPeriodId,
                        StartDate = firstPeriodEntry.FundingPeriodStartDate,
                        EndDate = firstPeriodEntry.FundingPeriodEndDate
                    }
                };

                IEnumerable<IGrouping<string, AllocationNotificationFeedIndex>> fundingStreamResultSummaryGroups = fundingPeriodResultSummaryGroup.GroupBy(m => m.FundingStreamId);

                foreach (IGrouping<string, AllocationNotificationFeedIndex> fundingStreamResultSummaryGroup in fundingStreamResultSummaryGroups)
                {
                    AllocationNotificationFeedIndex feedIndex = fundingStreamResultSummaryGroup.First();

                    FundingStreamResultSummary fundingStreamResultSummary = new FundingStreamResultSummary
                    {
                        FundingStream = new FundingStream
                        {
                            FundingStreamCode = feedIndex.FundingStreamId,
                            FundingStreamName = feedIndex.FundingStreamName
                        }
                    };

                    foreach (AllocationNotificationFeedIndex allocationFeedIndex in fundingStreamResultSummaryGroup)
                    {
                        fundingStreamResultSummary.Allocations.Add(new AllocationResult
                        {
                            AllocationLine = new AllocationLine
                            {
                                AllocationLineCode = allocationFeedIndex.AllocationLineId,
                                AllocationLineName = allocationFeedIndex.AllocationLineName
                            },
                            AllocationVersionNumber = (ushort)allocationFeedIndex.AllocationVersionNumber,
                            AllocationStatus = allocationFeedIndex.AllocationStatus,
                            AllocationAmount = Convert.ToDecimal(allocationFeedIndex.AllocationAmount)
                        });
                    }

                    fundingStreamResultSummary.TotalAmount = fundingStreamResultSummary.Allocations.Sum(m => m.AllocationAmount);

                    if (!string.IsNullOrWhiteSpace(feedIndex.PolicySummaries))
                    {
                        IEnumerable<PublishedProviderResultsPolicySummary> policySummaries = JsonConvert.DeserializeObject<IEnumerable<PublishedProviderResultsPolicySummary>>(feedIndex.PolicySummaries);

                        foreach (PublishedProviderResultsPolicySummary publishedPolicySummaryResult in policySummaries)
                        {
                            PolicyResult policyResult = new PolicyResult
                            {
                                Policy = new Policy
                                {
                                    PolicyId = publishedPolicySummaryResult.Policy.Id,
                                    PolicyName = publishedPolicySummaryResult.Policy.Name,
                                    PolicyDescription = publishedPolicySummaryResult.Policy.Description
                                }
                            };

                            foreach (PublishedProviderResultsCalculationSummary publishedCalculationSummary in publishedPolicySummaryResult.Calculations)
                            {
                                policyResult.Calculations.Add(new Models.CalculationResult
                                {
                                    CalculationName = publishedCalculationSummary.Name,
                                    CalculationVersionNumber = (ushort)publishedCalculationSummary.Version,
                                    CalculationType = publishedCalculationSummary.CalculationType.ToString(),
                                    CalculationAmount = publishedCalculationSummary.Amount
                                });
                            }

                            if (!publishedPolicySummaryResult.Policies.IsNullOrEmpty())
                            {
                                foreach (PublishedProviderResultsPolicySummary subPolicy in publishedPolicySummaryResult.Policies)
                                {
                                    PolicyResult subPolicyResult = new PolicyResult
                                    {
                                        Policy = new Policy
                                        {
                                            PolicyId = publishedPolicySummaryResult.Policy.Id,
                                            PolicyName = publishedPolicySummaryResult.Policy.Name,
                                            PolicyDescription = publishedPolicySummaryResult.Policy.Description
                                        }
                                    };

                                    foreach (PublishedProviderResultsCalculationSummary publishedCalculationSummary in subPolicy.Calculations)
                                    {
                                        subPolicyResult.Calculations.Add(new Models.CalculationResult
                                        {
                                            CalculationName = publishedCalculationSummary.Name,
                                            CalculationVersionNumber = (ushort)publishedCalculationSummary.Version,
                                            CalculationType = publishedCalculationSummary.CalculationType.ToString(),
                                            CalculationAmount = publishedCalculationSummary.Amount
                                        });
                                    }

                                    subPolicyResult.TotalAmount = subPolicyResult.Calculations.Where(m => m.CalculationType == "Funding").Sum(m => m.CalculationAmount);
                                    policyResult.SubPolicyResults.Add(subPolicyResult);
                                }
                            }

                            policyResult.TotalAmount = policyResult.Calculations.Where(m => m.CalculationType == "Funding").Sum(m => m.CalculationAmount);

                            foreach (PolicyResult subPolicyResult in policyResult.SubPolicyResults)
                            {
                                policyResult.TotalAmount = policyResult.TotalAmount + subPolicyResult.Calculations.Where(m => m.CalculationType == "Funding").Sum(m => m.CalculationAmount);
                            }

                            fundingStreamResultSummary.Policies.Add(policyResult);
                        }
                    }

                    providerResutSummary.TotalAmount += fundingStreamResultSummary.TotalAmount;

                    providerPeriodResultSummary.FundingStreamResults.Add(fundingStreamResultSummary);
                }

                providerResutSummary.FundingPeriodResults = providerResutSummary.FundingPeriodResults.Concat(new[] { providerPeriodResultSummary }).ToArraySafe();
            }

            return providerResutSummary;
        }

        private IEnumerable<AllocationNotificationFeedIndex> ValidateFeeds(IEnumerable<AllocationNotificationFeedIndex> feeds, bool checkProfiling = true, bool checkPolicySummaries = true)
        {
            IList<AllocationNotificationFeedIndex> validFeeds = new List<AllocationNotificationFeedIndex>();

            foreach(AllocationNotificationFeedIndex feed in feeds)
            {
                string logPrefix = $"Feed with id {feed.Id} contains missing data:";

                if (string.IsNullOrWhiteSpace(feed.ProviderId))
                {
                    _logger.Warning($"{logPrefix} provider id");

                    continue;
                }

                //temp until we fix ukprns
                if (string.IsNullOrWhiteSpace(feed.ProviderUkPrn))
                {
                    feed.ProviderUkPrn = feed.ProviderId;
                }

                if(string.IsNullOrWhiteSpace(feed.FundingPeriodId) || string.IsNullOrWhiteSpace(feed.FundingPeriodType))
                {
                    _logger.Warning($"{logPrefix} funding period id or type");

                    continue;
                }

                if(string.IsNullOrWhiteSpace(feed.FundingStreamId) || string.IsNullOrWhiteSpace(feed.FundingStreamName))
                {
                    _logger.Warning($"{logPrefix} funding stream id or name");

                    continue;
                }

                if (string.IsNullOrWhiteSpace(feed.AllocationLineId) || string.IsNullOrWhiteSpace(feed.AllocationLineName))
                {
                    _logger.Warning($"{logPrefix} allocation line id or name");

                    continue;
                }

                if (string.IsNullOrWhiteSpace(feed.PolicySummaries))
                {
                    _logger.Warning($"{logPrefix} policy summaries");

                    continue;
                }

                if (checkPolicySummaries)
                {
                    try
                    {
                        IEnumerable<PublishedProviderResultsPolicySummary> policySummaries = JsonConvert.DeserializeObject<IEnumerable<PublishedProviderResultsPolicySummary>>(feed.PolicySummaries);

                        if (policySummaries.IsNullOrEmpty())
                        {
                            _logger.Warning($"{logPrefix} policy summaries");

                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"Failed to deserialize policies for feed id {feed.Id}");

                        continue;
                    }
                }

                if (checkProfiling)
                {
                    try
                    {
                        IEnumerable<ProfilingPeriod> profiling = JsonConvert.DeserializeObject<IEnumerable<ProfilingPeriod>>(feed.ProviderProfiling);

                        if (profiling.IsNullOrEmpty())
                        {
                            _logger.Warning($"{logPrefix} provider profiles");

                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"Failed to deserialize provider profiles for feed id {feed.Id}");

                        continue;
                    }
                }

                validFeeds.Add(feed);
            }

            return validFeeds;
        }

        private IEnumerable<AllocationNotificationFeedIndex> ValidateFeedsForLaCode(IEnumerable<AllocationNotificationFeedIndex> feeds)
        {
            IList<AllocationNotificationFeedIndex> validFeeds = new List<AllocationNotificationFeedIndex>();

            foreach (AllocationNotificationFeedIndex feed in feeds)
            {
                string logPrefix = $"Feed with id {feed.Id} contains missing data:";

                if (string.IsNullOrWhiteSpace(feed.LaCode))
                {
                    _logger.Warning($"{logPrefix} la code");

                    continue;
                }

                if (string.IsNullOrWhiteSpace(feed.ProviderId))
                {
                    _logger.Warning($"{logPrefix} provider id");

                    continue;
                }

                //temp until we fix ukprns
                if (string.IsNullOrWhiteSpace(feed.ProviderUkPrn))
                {
                    feed.ProviderUkPrn = feed.ProviderId;
                }

                if (string.IsNullOrWhiteSpace(feed.FundingPeriodId) || string.IsNullOrWhiteSpace(feed.FundingPeriodType))
                {
                    _logger.Warning($"{logPrefix} funding period id or type");

                    continue;
                }

                if (string.IsNullOrWhiteSpace(feed.FundingStreamId) || string.IsNullOrWhiteSpace(feed.FundingStreamName))
                {
                    _logger.Warning($"{logPrefix} funding stream id or name");

                    continue;
                }

                if (string.IsNullOrWhiteSpace(feed.AllocationLineId) || string.IsNullOrWhiteSpace(feed.AllocationLineName))
                {
                    _logger.Warning($"{logPrefix} allocation line id or name");

                    continue;
                }

                try
                {
                    IEnumerable<ProfilingPeriod> profiling = JsonConvert.DeserializeObject<IEnumerable<ProfilingPeriod>>(feed.ProviderProfiling);

                    if (profiling.IsNullOrEmpty())
                    {
                        _logger.Warning($"{logPrefix} provider profiles");

                        continue;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Failed to deserialize provider profiles for feed id {feed.Id}");

                    continue;
                }

                validFeeds.Add(feed);
            }

            return validFeeds;
        }
    }
}
