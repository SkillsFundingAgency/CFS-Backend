using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class ProfilingInMemoryClient : IProfilingApiClient
    {
        public IEnumerable<ProfilingPeriod> ProfilingPeriods { get; set; }

        public IEnumerable<DistributionPeriods> DistributionPeriods { get; set; }

        public IList<(decimal? Value, string FundingStreamId, string FundingPeriodId, string FundingLineCode, IEnumerable<ProfilingPeriod> ProfilingPeriods, IEnumerable<DistributionPeriods> DistributionPeriods)> FundingValueProfileSplits { get; set; }

        public ProfilingInMemoryClient(IEnumerable<ProfilingPeriod> profilingPeriods, IEnumerable<DistributionPeriods> distributionPeriods, IList<(decimal? Value, string FundingStreamId, string FundingPeriodId, string FundingLineCode, IEnumerable<ProfilingPeriod> ProfilingPeriods, IEnumerable<DistributionPeriods> DistributionPeriods)> fundingValueProfileSplits)
        {
            ProfilingPeriods = profilingPeriods;
            DistributionPeriods = distributionPeriods;
            FundingValueProfileSplits = fundingValueProfileSplits;
        }

        public async Task<ValidatedApiResponse<ProviderProfilingResponseModel>> GetProviderProfilePeriods(ProviderProfilingRequestModel requestModel)
        {
            (decimal? Value, string FundingStreamId, string FundingPeriodId, string FundingLineCode, IEnumerable<ProfilingPeriod> ProfilingPeriods, IEnumerable<DistributionPeriods> DistributionPeriods) fundingValueProfileSplit = FundingValueProfileSplits.FirstOrDefault(_ => _.Value == requestModel.FundingValue && _.FundingStreamId == requestModel.FundingStreamId && _.FundingPeriodId == requestModel.FundingPeriodId && _.FundingLineCode == requestModel.FundingLineCode);
            return await Task.FromResult(new ValidatedApiResponse<ProviderProfilingResponseModel>(HttpStatusCode.OK, new ProviderProfilingResponseModel()
            {
                DeliveryProfilePeriods = fundingValueProfileSplit.Value.HasValue ? fundingValueProfileSplit.ProfilingPeriods : ProfilingPeriods,
                DistributionPeriods = fundingValueProfileSplit.Value.HasValue ? fundingValueProfileSplit.DistributionPeriods : DistributionPeriods
            }));
        }

        public Task<NoValidatedContentApiResponse> SaveProfilingConfig(SetFundingStreamPeriodProfilePatternRequestModel requestModel)
        {
            throw new NotImplementedException();
        }
    }
}
