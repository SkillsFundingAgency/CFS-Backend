using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.External;
using CalculateFunding.Models.External.AtomItems;
using Swashbuckle.AspNetCore.Examples;

namespace CalculateFunding.Api.External.ExampleProviders
{
    public class AllocationNotificationExamples : IExamplesProvider
    {
        public object GetExamples()
        {
            var baseUrl = new Uri($"https://api.calculate-funding.education.gov.uk/v1/finance");
            return new AtomFeed
            {
                Id = $"uuid:{Guid.NewGuid():N}",
                Title = "Calculate Funding Service Allocation Feed",
                Updated = DateTimeOffset.UtcNow,
                Author = new AtomAuthor
                {
                    Name = "Calculate Funding Service"
                },
                Rights = $"Copyright (C) {DateTime.Today.Year} Department for Education",
                Link = new List<AtomLink>
                {
                    new AtomLink{ Href = $"{baseUrl}?page=21", Rel = "self"},
                    new AtomLink{ Href = $"{baseUrl}", Rel = "first"},
                    new AtomLink{ Href = $"{baseUrl}?page=1067", Rel = "last"},
                    new AtomLink{ Href = $"{baseUrl}?page=11", Rel = "previous"},
                    new AtomLink{ Href = $"{baseUrl}?page=13", Rel = "next"},
                },
                AtomEntry = new List<AtomEntry>
                {
                    AllocationEntry("AY1819", new DateTime(2018, 9, 1), "63432", new FundingStream { FundingStreamCode = "YPLRE", FundingStreamName = "Academies General Annual Grant"}, new AllocationLine{ AllocationLineCode = "YPE01", AllocationLineName = "School Budget Share"}, 46283M, 2340 ),
                    AllocationEntry("AY1819", new DateTime(2018, 9, 1), "63432", new FundingStream { FundingStreamCode = "YPLRE", FundingStreamName = "Academies General Annual Grant"}, new AllocationLine{ AllocationLineCode = "YPE13", AllocationLineName = "Pupil Led Factors"}, 1623M, 340 )
                }
            };
        }



        internal static AtomEntry AllocationEntry(string periodId, DateTime periodStartDate, string providerId, FundingStream fundingStream, AllocationLine allocationLine, decimal amount, uint? count)
        {
            return new AtomEntry
            {
                Id = $"uuid:{Guid.NewGuid():N}",
                Title = "Allocation Pupil Led Factors was Approved",
                Summary = $"{{URPRN: 10000{providerId}, version: 3}}",
                Published = DateTimeOffset.UtcNow.AddDays(-1),
                Updated = DateTimeOffset.UtcNow,
                Content = new AtomContent
                {
                    Allocation = AllocationExamples.Allocation(periodId, periodStartDate, providerId, fundingStream, allocationLine, amount, count)

                }


            };
        }

    }
}
