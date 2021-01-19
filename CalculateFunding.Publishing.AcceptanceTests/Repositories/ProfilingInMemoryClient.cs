using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NSubstitute;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class ProfilingInMemoryClient : IProfilingApiClient
    {
        public IEnumerable<ProfilingPeriod> ProfilingPeriods { get; set; } = new ProfilingPeriod[0];

        public IEnumerable<DistributionPeriods> DistributionPeriods { get; set; } = new DistributionPeriods[0];

        public IList<(decimal? Value, 
            string FundingStreamId, 
            string FundingPeriodId, 
            string FundingLineCode, 
            IEnumerable<ProfilingPeriod> ProfilingPeriods, 
            IEnumerable<DistributionPeriods> DistributionPeriods)> FundingValueProfileSplits { get; set; } 
            = new List<(decimal? Value,
                string FundingStreamId, 
                string FundingPeriodId, 
                string FundingLineCode, 
                IEnumerable<ProfilingPeriod> ProfilingPeriods, 
                IEnumerable<DistributionPeriods> DistributionPeriods)>();

        public async Task<ValidatedApiResponse<ProviderProfilingResponseModel>> GetProviderProfilePeriods(ProviderProfilingRequestModel requestModel)
        {
            (decimal? Value, string FundingStreamId, string FundingPeriodId, string FundingLineCode, IEnumerable<ProfilingPeriod> ProfilingPeriods, IEnumerable<DistributionPeriods> DistributionPeriods) fundingValueProfileSplit 
                = FundingValueProfileSplits.FirstOrDefault(_ => _.Value == requestModel.FundingValue && _.FundingStreamId == requestModel.FundingStreamId && _.FundingPeriodId == requestModel.FundingPeriodId && _.FundingLineCode == requestModel.FundingLineCode);
            
            return await Task.FromResult(new ValidatedApiResponse<ProviderProfilingResponseModel>(HttpStatusCode.OK, new ProviderProfilingResponseModel()
            {
                DeliveryProfilePeriods = fundingValueProfileSplit.Value.HasValue ? fundingValueProfileSplit.ProfilingPeriods : ProfilingPeriods,
                DistributionPeriods = fundingValueProfileSplit.Value.HasValue ? fundingValueProfileSplit.DistributionPeriods : DistributionPeriods
            }));
        }

        public void AddFundingValueProfileSplit((decimal? Value,
            string FundingStreamId,
            string FundingPeriodId,
            string FundingLineCode,
            IEnumerable<ProfilingPeriod> ProfilingPeriods,
            IEnumerable<DistributionPeriods> DistributionPeriods) fundingValueSplit)
        {
            FundingValueProfileSplits.Add(fundingValueSplit);
        }

        public Task<NoValidatedContentApiResponse> SaveProfilingConfig(SetFundingStreamPeriodProfilePatternRequestModel requestModel)
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> CreateProfilePattern(CreateProfilePatternRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> EditProfilePattern(EditProfilePatternRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> DeleteProfilePattern(string id)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<FundingStreamPeriodProfilePattern>> GetProfilePattern(string id)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<FundingStreamPeriodProfilePattern>>> GetProfilePatternsForFundingStreamAndFundingPeriod(string fundingStreamId, string fundingPeriodId)
        {
            throw new NotImplementedException();
        }

        public Task<(bool Ok, string Message)> IsHealthOk()
        {
            return Task.FromResult((true, string.Empty));
        }

        public Task<ApiResponse<ReProfileResponse>> ReProfile(ReProfileRequest request) => throw new NotImplementedException();
        

        public Task<ApiResponse<IEnumerable<ReProfilingStrategyResponse>>> GetAllReProfilingStrategies() => throw new NotImplementedException();

        public Task<ValidatedApiResponse<IEnumerable<BatchProfilingResponseModel>>> GetBatchProfilePeriods(BatchProfilingRequestModel requestModel)
        {
            IEnumerable<(decimal fundingValue, Task<ValidatedApiResponse<ProviderProfilingResponseModel>> response)> profilingResponses =
                requestModel.FundingValues.Select(fundingValue => (fundingValue, GetProviderProfilePeriods(new ProviderProfilingRequestModel
            {
                FundingStreamId = requestModel.FundingStreamId,
                FundingPeriodId = requestModel.FundingPeriodId,
                FundingValue = fundingValue,
                ProviderType = requestModel.ProviderType,
                ProviderSubType = requestModel.ProviderSubType,
                FundingLineCode = requestModel.FundingLineCode,
                ProfilePatternKey = requestModel.ProfilePatternKey
            })));

            ValidatedApiResponse<IEnumerable<BatchProfilingResponseModel>> batchResponses = new ValidatedApiResponse<IEnumerable<BatchProfilingResponseModel>>(HttpStatusCode.OK,
                profilingResponses.Select(_ => new BatchProfilingResponseModel
                {
                    Key = $"{requestModel.FundingPeriodId}-{requestModel.FundingStreamId}-{requestModel.ProfilePatternKey ?? "?"}-{requestModel.ProviderType ?? "?"}-{requestModel.ProviderSubType ?? "?"}-{requestModel.FundingLineCode}-{_.fundingValue:N4}",
                    DistributionPeriods = _.response.Result.Content.DistributionPeriods,
                    DeliveryProfilePeriods = _.response.Result.Content.DeliveryProfilePeriods,
                    FundingValue = _.fundingValue,
                    ProfilePatternKey = _.response.Result.Content.ProfilePatternKey,
                    ProfilePatternDisplayName = _.response.Result.Content.ProfilePatternDisplayName
                }));

            return Task.FromResult(batchResponses);
        }
    }
}
