using System;
using System.Collections.Generic;
using CalculateFunding.Models.External;
using Swashbuckle.AspNetCore.Examples;

namespace CalculateFunding.Api.External.V1.Models.Examples
{
    public class PolicyResultCalculationExamples : IExamplesProvider
    {
        public object GetExamples()
        {
            return new ProviderPolicyResult
            {

                Provider = new Provider
                {
                    LegalName = "Provider name",
                    Ukprn = "12341234",
                    ProviderOpenDate = new DateTime(2016, 4, 1)
                },
                Period = new Period
                {
                    PeriodType = "AY",
                    PeriodId = "AY1718",
                    StartDate = new DateTime(2017, 9, 1),
                    EndDate = new DateTime(2018, 8, 30)
                },
                PolicyResult = new PolicyResult
                {
                    Policy = new Policy
                    {
                        PolicyId = "2434567890XXX0987654342",
                        PolicyName = "School Block Share",
                        PolicyDescription = "Policy description...."
                    },
                    TotalAmount = 1500000M,
                    SubPolicyResults = new List<PolicyResult>
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
                                    CalculationStatus = "published",
                                    //SchemaVersion = 0.01M
                                },

                                new CalculationResult
                                {
                                    CalculationName = "Calculation Two Count",
                                    CalculationAmount = 500000M,
                                    CalculationVersionNumber = 8,
                                    CalculationStatus = "published",
                                    //SchemaVersion = 0.01M
                                },

                                new CalculationResult
                                {
                                    CalculationName = "Calculation Three Rate",
                                    CalculationAmount = 500000M,
                                    CalculationVersionNumber = 2,
                                    CalculationStatus = "published",
                                    //SchemaVersion = 0.01M
                                },

                            }
                        }
                    }
                }
            };
        }
    }

public class FundingStreamResultCalculationExamples : IExamplesProvider
    {
        public object GetExamples()
        {
            return new ProviderFundingStreamResult
            {
                FundingStream = new FundingStream {
                    FundingStreamCode = "YPLRE",
                    FundingStreamName = "Academies General Annual Grant"
                },
                Provider = new Provider { LegalName = "Provider name", Ukprn = "12341234", ProviderOpenDate = new DateTime(2016, 4, 1) },
                Period = new Period
                {
                    PeriodType = "AY",
                    PeriodId = "AY1718",
                    StartDate = new DateTime(2017, 9, 1),
                    EndDate = new DateTime(2018, 8, 30)
                },
                PolicyResults = new List<PolicyResult>()
                {
                    new PolicyResult
                    {
                        Policy = new Policy
                        {
                            PolicyName = "Basic entitlement",
                            PolicyDescription = "Policy description...."
                        },
                        Calculations = new List<CalculationResult>()
                        {
                            new CalculationResult
                            {
                                CalculationName = "Calculation One Amount",
                                CalculationAmount = 500000M,
                                CalculationVersionNumber = 5,
                                CalculationStatus = "published",
                                //SchemaVersion = 0.01M
                            },

                            new CalculationResult
                            {
                                CalculationName = "Calculation Two Count",
                                CalculationAmount = 500000M,
                                CalculationVersionNumber = 8,
                                CalculationStatus = "published",
                                //SchemaVersion = 0.01M
                            },

                            new CalculationResult
                            {
                                CalculationName = "Calculation Three Rate",
                                CalculationAmount = 500000M,
                                CalculationVersionNumber = 2,
                                CalculationStatus = "published",
                                //SchemaVersion = 0.01M
                            },

                        }
                    }
                }


            };
        }
    }
}