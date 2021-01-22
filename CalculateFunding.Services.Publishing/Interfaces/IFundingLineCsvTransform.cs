using System.Collections.Generic;
using System.Dynamic;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IFundingLineCsvTransform
    {
        bool IsForJobType(FundingLineCsvGeneratorJobType jobType);

        string FundingLineCode {set;}
        
        IEnumerable<ExpandoObject> Transform(IEnumerable<dynamic> documents);
    }
}