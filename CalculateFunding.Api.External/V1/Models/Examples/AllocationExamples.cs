using System;
using System.Collections.Generic;
using CalculateFunding.Models.External;
using Swashbuckle.AspNetCore.Examples;

namespace CalculateFunding.Api.External.V1.Models.Examples
{
    public class AllocationExamples : IExamplesProvider
    {
        public object GetExamples()
        {
            return Allocation("AY1819", new DateTime(2018, 9, 1), "63432",
                new AllocationFundingStreamModel { FundingStreamCode = "YPLRE", FundingStreamName = "Academies General Annual Grant"},
                new AllocationLine {AllocationLineCode = "YPE13", AllocationLineName = "Pupil Led Factors"}, 1623M,
                340, Guid.NewGuid().ToString("N"));

        }

        internal static AllocationModel Allocation(string periodId, DateTime periodStartDate, string providerId,
            AllocationFundingStreamModel fundingStream, AllocationLine allocationLine, decimal amount, int? count, string allocationResultId)
        {
            return new AllocationModel
            {
                AllocationResultId = allocationResultId,
                FundingStream = fundingStream,
                AllocationLine = allocationLine,
                AllocationVersionNumber = 3,
                AllocationStatus = "publsihed",
                Provider = new AllocationProviderModel
                {
                    Ukprn = $"10000{providerId}",
                    Upin = $"100{providerId}",
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