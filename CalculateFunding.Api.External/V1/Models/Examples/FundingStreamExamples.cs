using System.Collections.Generic;
using Swashbuckle.AspNetCore.Examples;

namespace CalculateFunding.Api.External.V1.Models.Examples
{
    public class FundingStreamExamples : IExamplesProvider
    {
        public object GetExamples()
        {
            return new List<FundingStream>
            {
                new FundingStream { FundingStreamCode = "YPLRE", FundingStreamName = "Academies General Annual Grant", AllocationLines = new List<AllocationLine>()
                {                 
                    new AllocationLine{ AllocationLineCode = "YPE01", AllocationLineName = "School Budget Share"},
                    new AllocationLine{ AllocationLineCode = "YPE13", AllocationLineName = "Pupil Led Factors"},
                }},
                new FundingStream { FundingStreamCode = "YPLRP", FundingStreamName = "DSG", AllocationLines = new List<AllocationLine>()
                {
                    new AllocationLine{ AllocationLineCode = "YPP01", AllocationLineName = "DSG Allocations"}
                }},
            };
        }
    }
}