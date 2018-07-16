using System;
using System.Collections.Generic;
using CalculateFunding.Models.External;
using Swashbuckle.AspNetCore.Examples;

namespace CalculateFunding.Api.External.ExampleProviders
{
    public class ProviderResultSummaryExamples : IExamplesProvider
    {
        public object GetExamples()
        {
            return new ProviderResultSummary
            {
                Provider = new Provider {LegalName = "", Ukprn = "", ProviderOpenDate = new DateTime(2016, 4, 1)},
                Period = new Period
                {
                    PeriodType = "AY",
                    PeriodId = "AY1718",
                    StartDate = new DateTime(2017, 9, 1),
                    EndDate = new DateTime(2018, 8, 30)
                },
                FundingStreamResults = new List<FundingStreamResultSummary>()
                {
                    new FundingStreamResultSummary
                    {
                        FundingStream =
                            new FundingStream
                            {
                                FundingStreamCode = "YPLRE",
                                FundingStreamName = "Academies General Annual Grant"
                            },
                        TotalAmount = 1500000M,
                        Allocations = new List<AllocationResult>()
                        {
                            new AllocationResult
                            {
                                AllocationLine = new AllocationLine
                                {
                                    AllocationLineCode = "YPE01",
                                    AllocationLineName = "School Budget Share"
                                },
                                AllocationAmount = 1000000M,
                                AllocationVersionNumber = 3,
                                AllocationStatus = "published"
                                //SchemaVersion = 0.01M
                            },
                            new AllocationResult
                            {
                                AllocationLine = new AllocationLine
                                {
                                    AllocationLineCode = "YPE13",
                                    AllocationLineName = "Pupil Led Factors"
                                },
                                AllocationAmount = 500000M,
                                AllocationVersionNumber = 5,
                                AllocationStatus = "published"
                                //SchemaVersion = 0.01M
                            }
                        },
                        Policies = new List<PolicyResult>()
                        {
                            new PolicyResult
                            {
                                Policy = new Policy
                                {
                                    PolicyId = "1234567890XXX0987654321",
                                    PolicyName = "Basic entitlement",
                                    PolicyDescription = "Policy description...."
                                },
                                TotalAmount = 1500000M,
                                Calculations = new List<CalculationResult>()
                                {
                                    new CalculationResult
                                    {
                                        CalculationName = "Calculation One Amount",
                                        CalculationAmount = 500000M,
                                        CalculationVersionNumber = 5,
                                        CalculationStatus = "published"
                                        //SchemaVersion = 0.01M
                                    },

                                    new CalculationResult
                                    {
                                        CalculationName = "Calculation Two Count",
                                        CalculationAmount = 500000M,
                                        CalculationVersionNumber = 8,
                                        CalculationStatus = "published"
                                        //SchemaVersion = 0.01M
                                    },

                                    new CalculationResult
                                    {
                                        CalculationName = "Calculation Three Rate",
                                        CalculationAmount = 500000M,
                                        CalculationVersionNumber = 2,
                                        CalculationStatus = "published"
                                        //SchemaVersion = 0.01M
                                    }
                                }
                            }
                        }
                    },
                    new FundingStreamResultSummary
                    {
                        FundingStream =
                            new FundingStream
                            {
                                FundingStreamCode = "YPLRP",
                                FundingStreamName = "DSG"
                            },
                        TotalAmount = 500000M,
                        Allocations = new List<AllocationResult>()
                        {
                            new AllocationResult
                            {
                                AllocationLine = new AllocationLine
                                {
                                    AllocationLineCode = "YPP01",
                                    AllocationLineName = "DSG Allocations"
                                },
                                AllocationAmount = 1000000M,
                                AllocationVersionNumber = 3,
                                AllocationStatus = "published"
                                //SchemaVersion = 0.01M
                            }
                        }
                    }
                }
            };
        }
    }
}