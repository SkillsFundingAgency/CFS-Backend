using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Services.Profiling.Models;

namespace CalculateFunding.Services.Profiling.Repositories
{
	public interface IProfilePatternRepository
    {
        Task<FundingStreamPeriodProfilePattern> GetProfilePattern(string fundingPeriodId, 
            string fundingStreamId, 
            string fundingLineCode,
            string profilePatternKey);

        Task<HttpStatusCode> SaveFundingStreamPeriodProfilePattern(FundingStreamPeriodProfilePattern fundingStreamPeriodProfilePattern);
        Task<HttpStatusCode> DeleteProfilePattern(string id);
        Task<FundingStreamPeriodProfilePattern> GetProfilePattern(string id);

        Task<IEnumerable<FundingStreamPeriodProfilePattern>> GetProfilePatternsForFundingStreamAndFundingPeriod(string fundingStreamId,
            string fundingPeriodId);
        Task<FundingStreamPeriodProfilePattern> GetProfilePattern(string fundingPeriodId, string fundingStreamId, string fundingLineCode, string providerType, string providerSubType);
    }
}