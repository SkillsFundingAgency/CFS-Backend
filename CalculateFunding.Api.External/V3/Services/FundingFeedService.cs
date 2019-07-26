using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Api.External.Swagger.Helpers;
using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Api.External.V3.Models;
using CalculateFunding.Models.External.V3.AtomItems;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Models.Search;
using CalculateFunding.Services.Publising.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CalculateFunding.Api.External.V3.Services
{
    public class FundingFeedService : IFundingFeedService
    {
        const int MaxRecords = 500;

        private readonly IFundingFeedSearchService _feedService;

        public FundingFeedService(IFundingFeedSearchService feedService)
        {
            _feedService = feedService;
        }

        public async Task<IActionResult> GetFunding(HttpRequest request, int? pageRef, IEnumerable<string> fundingStreamIds = null, IEnumerable<string> fundingPeriodIds = null,  IEnumerable<GroupingReason> groupingReasons = null, int? pageSize = MaxRecords)
        {
            pageSize = pageSize ?? MaxRecords;
            
            if (pageRef < 1)
            {
                return new BadRequestObjectResult("Page ref should be at least 1");
            }

            if (pageSize < 1 || pageSize > 500)
            {
                return new BadRequestObjectResult($"Page size should be more that zero and less than or equal to {MaxRecords}");
            }

            SearchFeedV3<PublishedFundingIndex> searchFeed = await _feedService.GetFeedsV3(pageRef, pageSize.Value, fundingStreamIds, fundingPeriodIds, groupingReasons?.Select(x => x.ToString()));

            if (searchFeed == null || searchFeed.TotalCount == 0 || searchFeed.Entries.IsNullOrEmpty())
            {
                return new NotFoundResult();
            }

            AtomFeed<AtomEntry> atomFeed = CreateAtomFeed(searchFeed, request);

            return Formatter.ActionResult<AtomFeed<AtomEntry>>(request, atomFeed);
        }

        private AtomFeed<AtomEntry> CreateAtomFeed(SearchFeedV3<PublishedFundingIndex> searchFeed, HttpRequest request)
        {
            const string fundingEndpointName = "notifications";
            string baseRequestPath = request.Path.Value.Substring(0, request.Path.Value.IndexOf(fundingEndpointName, StringComparison.Ordinal) + fundingEndpointName.Length);
            string fundingTrimmedRequestPath = baseRequestPath.Replace(fundingEndpointName, string.Empty).TrimEnd('/');

            string queryString = request.QueryString.Value;

            string fundingUrl = $"{request.Scheme}://{request.Host.Value}{baseRequestPath}{{0}}{(!string.IsNullOrWhiteSpace(queryString) ? queryString : "")}";

            AtomFeed<AtomEntry> atomFeed = new AtomFeed<AtomEntry>
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = "Calculate Funding Service Funding Feed",
                Author = new CalculateFunding.Models.External.AtomItems.AtomAuthor
                {
                    Name = "Calculate Funding Service",
                    Email = "calculate-funding@education.gov.uk"
                },
                Updated = DateTimeOffset.Now,
                Rights = "Copyright (C) 2019 Department for Education",
                Link = searchFeed.GenerateAtomLinksForResultGivenBaseUrl(fundingUrl).ToList(),
                AtomEntry = new List<AtomEntry>(),
                IsArchived = searchFeed.IsArchivePage
            };
            
            foreach (PublishedFundingIndex feedIndex in searchFeed.Entries)
            {
                string link = $"{request.Scheme}://{request.Host.Value}{fundingTrimmedRequestPath}/byId/{feedIndex.Id}";

                atomFeed.AtomEntry.Add(new AtomEntry
                {
                    Id = link,
                    Title = feedIndex.Id,
                    Summary = feedIndex.Id,
                    Published = feedIndex.StatusChangedDate,
                    Updated = feedIndex.StatusChangedDate.Value,
                    Version = feedIndex.Version,
                    Link = new CalculateFunding.Models.External.AtomItems.AtomLink("Funding", link),
                    Content = new
                        {
                            feedIndex.FundingStreamId,
                            feedIndex.FundingPeriodId,
                            feedIndex.IdentifierType
                        }
                });
            }

            return atomFeed;
        }

    }
}
