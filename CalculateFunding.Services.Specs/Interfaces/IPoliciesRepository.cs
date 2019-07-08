using CalculateFunding.Models.Policy;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface IPoliciesRepository
    {
        Task<Period> GetPeriodById(string periodId);
        Task<FundingStream> GetFundingStreamById(string fundingStreamId);
        Task<IEnumerable<Period>> GetPeriods();
        Task<HttpStatusCode> SaveFundingStream(FundingStream fundingStream);
        Task<IEnumerable<FundingStream>> GetFundingStreams(Expression<Func<FundingStream, bool>> query = null);
        Task SavePeriods(IEnumerable<Period> periods);
    }
}
