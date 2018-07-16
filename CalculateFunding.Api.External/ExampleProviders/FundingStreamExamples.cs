using System.Collections.Generic;
using CalculateFunding.Models.External;
using Swashbuckle.AspNetCore.Examples;

namespace CalculateFunding.Api.External.ExampleProviders
{
    public class FundingStreamExamples : IExamplesProvider
    {
        public object GetExamples()
        {
            return new List<FundingStream>
            {
                   
                new FundingStream { FundingStreamCode = "YPLRE", FundingStreamName = "Academies General Annual Grant", AllocationLines = new []
                {                 
                    new AllocationLine{ AllocationLineCode = "YPE01", AllocationLineName = "School Budget Share"},
                    new AllocationLine{ AllocationLineCode = "YPE13", AllocationLineName = "Pupil Led Factors"},
                }},
                new FundingStream { FundingStreamCode = "YPLRP", FundingStreamName = "DSG", AllocationLines = new []
                {
                    new AllocationLine{ AllocationLineCode = "YPP01", AllocationLineName = "DSG Allocations"}
                }},
            };
        }
    }
}