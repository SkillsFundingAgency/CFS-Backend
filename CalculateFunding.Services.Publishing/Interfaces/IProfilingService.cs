using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IProfilingService
    {
        Task<IEnumerable<ProfilePatternKey>> ProfileFundingLines(IEnumerable<FundingLine> fundingLines, string fundingStreamId, string fundingPeriodId, IEnumerable<ProfilePatternKey> profilePatternKeys = null, string providerType = null, string providerSubType = null);
    }
}
