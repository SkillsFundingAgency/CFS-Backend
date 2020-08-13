using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Policy.FundingPolicy;

namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface IPolicyRepository
    {
        Task<FundingConfiguration> GetFundingConfiguration(string configId);

        Task<HttpStatusCode> SaveFundingConfiguration(FundingConfiguration configuration);

        Task<IEnumerable<FundingStream>> GetFundingStreams(Expression<Func<DocumentEntity<FundingStream>, bool>> query = null);

        Task<FundingStream> GetFundingStreamById(string fundingStreamId);

        Task<HttpStatusCode> SaveFundingStream(FundingStream fundingStream);

        Task<FundingPeriod> GetFundingPeriodById(string fundingPeriodId);

        Task<IEnumerable<FundingPeriod>> GetFundingPeriods(Expression<Func<DocumentEntity<FundingPeriod>, bool>> query = null);

        Task SaveFundingPeriods(IEnumerable<FundingPeriod> fundingPeriods);

        Task<IEnumerable<FundingConfiguration>> GetFundingConfigurationsByFundingStreamId(string fundingStreamId);

        Task<IEnumerable<FundingConfiguration>> GetFundingConfigurations();

        Task<FundingDate> GetFundingDate(string fundingDateId);

        Task<HttpStatusCode> SaveFundingDate(FundingDate fundingDate);
    }
}
