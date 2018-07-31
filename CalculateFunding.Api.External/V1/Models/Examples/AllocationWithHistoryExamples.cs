using System;
using System.Collections.Generic;
using CalculateFunding.Models.External;
using Swashbuckle.AspNetCore.Examples;

namespace CalculateFunding.Api.External.V1.Models.Examples
{
    public class AllocationWithHistoryExamples : IExamplesProvider
    {
        public object GetExamples()
        {
            return AllocationWithHistory("AY1819", new DateTime(2018, 9, 1), "63432",
                new AllocationFundingStreamModel { FundingStreamCode = "YPLRE", FundingStreamName = "Academies General Annual Grant"},
                new AllocationLine {AllocationLineCode = "YPE13", AllocationLineName = "Pupil Led Factors"}, 1623M,
                340, Guid.NewGuid().ToString("N"));

        }

        internal static AllocationWithHistoryModel AllocationWithHistory(string periodId, DateTime periodStartDate, string providerId,
            AllocationFundingStreamModel fundingStream, AllocationLine allocationLine, decimal amount, int? count, string allocationResultId)
        {
            return new AllocationWithHistoryModel
            {
                AllocationResultId = allocationResultId,
                FundingStream = fundingStream,
                AllocationLine = allocationLine,
                AllocationVersionNumber = 3,
                AllocationStatus = "Published",
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
                ProfilePeriods = new[]
                {
                    new ProfilePeriod
                    {
                        Period = "Oct",
                        Occurrence = 1,
                        PeriodYear = "2018",
                        PeriodType = "CalendarMonth",
                        ProfileValue = amount /2,
                        DistributionPeriod = "2018-2019"
                    },
                    new ProfilePeriod
                    {
                        Period = "Apr",
                        Occurrence = 1,
                        PeriodYear = "2019",
                        PeriodType = "CalendarMonth",
                        ProfileValue = amount /2,
                        DistributionPeriod = "2018-2019"
                    }
                },
                History = new[]
                {
                    new AllocationHistoryModel
                    {
                         AllocationAmount = amount,
                         AllocationVersionNumber = 3,
                         Comment = "A test comment",
                         Author = "caclulate funding",
                         Date = DateTimeOffset.Now.AddDays(-1),
                         Status = "Published"
                    },
                    new AllocationHistoryModel
                    {
                         AllocationAmount = amount,
                         AllocationVersionNumber = 2,
                         Comment = "A test comment",
                         Author = "caclulate funding",
                         Date = DateTimeOffset.Now.AddDays(-2),
                          Status = "Approved"
                    },
                    new AllocationHistoryModel
                    {
                         AllocationAmount = amount,
                         AllocationVersionNumber = 2,
                         Comment = "A test comment",
                         Author = "caclulate funding",
                         Date = DateTimeOffset.Now.AddDays(-3),
                         Status = "Held"
                    }
                }
                
            };
        }
    }
}