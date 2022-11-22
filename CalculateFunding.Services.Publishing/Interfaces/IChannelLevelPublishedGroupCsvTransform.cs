using System.Collections.Generic;
using System.Dynamic;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IChannelLevelPublishedGroupCsvTransform
    {
        bool IsForJobDefinition(FundingLineCsvGeneratorJobType jobType);
        IEnumerable<ExpandoObject> Transform(
                                        IEnumerable<dynamic> documents,
                                        FundingLineCsvGeneratorJobType jobType,
                                        IEnumerable<ProfilePeriodPattern> profilePatterns = null,
                                        IEnumerable<string> distinctFundingLineNames = null);
    }
}
