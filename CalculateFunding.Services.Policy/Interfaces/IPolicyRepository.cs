using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.FundingPolicy;
using CalculateFunding.Models.Policy;

namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface IPolicyRepository
    {
        Task<FundingConfiguration> GetFundingConfiguration(string configId);
        Task<HttpStatusCode> SaveFundingConfiguration(FundingConfiguration configuration);
        Task<IEnumerable<FundingStream>> GetFundingStreams(Expression<Func<FundingStream, bool>> query = null);
        Task<FundingStream> GetFundingStreamById(string fundingStreamId);
        Task<HttpStatusCode> SaveFundingStream(FundingStream fundingStream);
        Task<Period> GetFundingPeriodById(string fundingPeriodId);
        Task<IEnumerable<Period>> GetFundingPeriods(Expression<Func<Period, bool>> query = null);
        Task SaveFundingPeriods(IEnumerable<Period> fundingPeriods);
    }
}
