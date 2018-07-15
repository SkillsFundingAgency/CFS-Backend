using System;
using CalculateFunding.Models.External;
using Swashbuckle.AspNetCore.Examples;

namespace CalculateFunding.Api.External.ExampleProviders
{
    public class AllocationExamples : IExamplesProvider
    {
        public object GetExamples()
        {
            return Allocation("AY1819", new DateTime(2018, 9, 1), "63432",
                new FundingStream {FundingStreamCode = "YPLRE", FundingStreamName = "Academies General Annual Grant"},
                new AllocationLine {AllocationLineCode = "YPE13", AllocationLineName = "Pupil Led Factors"}, 1623M,
                340);

        }

        internal static Allocation Allocation(string periodId, DateTime periodStartDate, string providerId, FundingStream fundingStream, AllocationLine allocationLine, decimal amount, uint? count)
        {
            return new Allocation
            {
                FundingStream = fundingStream,
                AllocationLine = allocationLine,
                AllocationVersionNumber = 3,
                AllocationStatus = "publsihed",
                Provider = new Provider
                {
                    UKPRN = $"10000{providerId}",
                    UPIN = $"100{providerId}",
                    ProviderOpenDate = new DateTime(2016, 9, 1),
                },
                Period = new Period
                {
                    PeriodType = "AY",
                    PeriodId = periodId,
                    StartDate = periodStartDate,
                    EndDate = periodStartDate.AddDays(-1)
                },
                AllocationAmount = amount,
                AllocationLearnerCount = count,
                //SchemaVersion = 0.01M
            };
        }
    }
}