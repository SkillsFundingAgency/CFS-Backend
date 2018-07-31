using CalculateFunding.Api.External.Swagger.Helpers;
using CalculateFunding.Api.External.V1.Interfaces;
using CalculateFunding.Api.External.V1.Models;
using CalculateFunding.Models.External;
using CalculateFunding.Models.External.AtomItems;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Search;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V1.Services
{
    public class AllocationNotificationFeedsService : IAllocationNotificationFeedsService
    {
        const int MaxRecords = 500;

        private readonly IAllocationNotificationsFeedsSearchService _feedsService;

        public AllocationNotificationFeedsService(IAllocationNotificationsFeedsSearchService feedsService)
        {
            _feedsService = feedsService;
        }

        public async Task<IActionResult> GetNotifications(int? pageRef, string allocationStatuses, HttpRequest request)
        {
            if (!pageRef.HasValue)
            {
                pageRef = 1;
            }

            if (pageRef < 1)
            {
                return new BadRequestObjectResult("Page ref should be at least 1");
            }

            string[] statusesArray = statusesArray = new[] { "Published" };

            if (!string.IsNullOrWhiteSpace(allocationStatuses))
            {
                statusesArray = allocationStatuses.Split(",");
            }

            SearchFeed<AllocationNotificationFeedIndex> searchFeed = await _feedsService.GetFeeds(pageRef.Value, MaxRecords, statusesArray);

            if(searchFeed == null || searchFeed.TotalCount == 0)
            {
                return new NotFoundResult();
            }

            AtomFeed<AllocationModel> atomFeed = CreateAtomFeed(searchFeed, request);

            return Formatter.ActionResult<AtomFeed<AllocationModel>>(request, atomFeed);
        }

        AtomFeed<AllocationModel> CreateAtomFeed(SearchFeed<AllocationNotificationFeedIndex> searchFeed, HttpRequest request)
        {
            string trimmedRequestPath = request.Path.Value.Substring(0, request.Path.Value.LastIndexOf("/", StringComparison.Ordinal));

            string allocationTrimmedRequestPath = trimmedRequestPath.Replace("notifications", "");

            string notificationsUrl = $"{request.Scheme}://{request.Host.Value}{trimmedRequestPath}/{{0}}{(request.QueryString.HasValue ? request.QueryString.Value : "")}";

            AtomFeed<AllocationModel> atomFeed = new AtomFeed<AllocationModel>
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = "Calculate Funding Service Allocation Feed",
                Author = new AtomAuthor
                {
                    Name = "Calculate Funding Service"
                },
                Updated = DateTimeOffset.Now,
                Rights = "Copyright (C) 2018 Department for Education",
                Link = new List<AtomLink>
                {
                    new AtomLink(string.Format(notificationsUrl, searchFeed.Self), "self"),
                    new AtomLink(string.Format(notificationsUrl, 1), "first"),
                    new AtomLink(string.Format(notificationsUrl, searchFeed.Last), "last"),
                    new AtomLink(string.Format(notificationsUrl, searchFeed.Previous), "previous"),
                    new AtomLink(string.Format(notificationsUrl, searchFeed.Next), "next"),
                },
                AtomEntry = new List<AtomEntry<AllocationModel>>()
            };

            foreach (AllocationNotificationFeedIndex feedIndex in searchFeed.Entries)
            {
                atomFeed.AtomEntry.Add(new AtomEntry<AllocationModel>
                {
                    Id = feedIndex.Id,
                    Title = feedIndex.Title,
                    Summary = feedIndex.Summary,
                    Published = feedIndex.DatePublished,
                    Updated = feedIndex.DateUpdated.Value,
                    Version = feedIndex.AllocationVersionNumber.ToString(),
                    Link = feedIndex.AllocationStatus == "Published" ? new AtomLink("Allocation", $"{ request.Scheme }://{request.Host.Value}{allocationTrimmedRequestPath}{feedIndex.Id}") : null,
                    Content = new AtomContent<AllocationModel>
                    {
                        Allocation = new AllocationModel
                        {
                            FundingStream = new AllocationFundingStreamModel
                            {
                                FundingStreamCode = feedIndex.FundingStreamId,
                                FundingStreamName = feedIndex.FundingStreamName
                            },
                            Period = new Period
                            {
                                PeriodType = feedIndex.FundingPeriodType,
                                PeriodId = feedIndex.FundingPeriodId,
                                StartDate = feedIndex.FundingPeriodStartDate,
                                EndDate = feedIndex.FundingPeriodEndDate
                            },
                            Provider = new AllocationProviderModel
                            {
                                Ukprn = feedIndex.ProviderUkPrn,
                                Upin = feedIndex.ProviderUpin,
                                ProviderOpenDate = feedIndex.ProviderOpenDate
                            },
                            AllocationLine = new AllocationLine
                            {
                                AllocationLineCode = feedIndex.AllocationLineId,
                                AllocationLineName = feedIndex.AllocationLineName
                            },
                            AllocationVersionNumber = feedIndex.AllocationVersionNumber,
                            AllocationStatus = feedIndex.AllocationStatus,
                            AllocationAmount = (decimal)feedIndex.AllocationAmount,
                            AllocationLearnerCount = feedIndex.AllocationLearnerCount,
                            AllocationResultId = feedIndex.Id,
                            ProfilePeriods = JsonConvert.DeserializeObject<IEnumerable<ProfilingPeriod>>(feedIndex.ProviderProfiling).Select(
                                    m => new ProfilePeriod(m.Period, m.Occurrence, m.Year.ToString(), m.Type, m.Value, m.DistributionPeriod)).ToArraySafe()
                        }
                    }
                });
            }

            return atomFeed;
        }
    }
}
