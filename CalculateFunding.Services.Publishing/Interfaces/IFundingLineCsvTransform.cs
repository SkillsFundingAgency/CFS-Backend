using System.Collections.Generic;
using System.Dynamic;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IFundingLineCsvTransform
    {
        bool IsForJobType(FundingLineCsvGeneratorJobType jobType);
        
        IEnumerable<ExpandoObject> Transform(
            IEnumerable<dynamic> documents, 
            FundingLineCsvGeneratorJobType jobType, 
            IEnumerable<ProfilePeriodPattern> profilePatterns = null,
            IEnumerable<string> distinctFundingLineNames = null);
    }
}