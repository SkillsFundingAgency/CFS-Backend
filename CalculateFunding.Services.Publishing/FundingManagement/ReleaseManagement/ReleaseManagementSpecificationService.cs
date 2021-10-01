using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class ReleaseManagementSpecificationService : IReleaseManagementSpecificationService
    {
        private readonly IReleaseManagementRepository _repo;
        private readonly IReleaseToChannelSqlMappingContext _releaseToChannelSqlMappingContext;
        private readonly AsyncPolicy _policyClientPolicy;
        private readonly IPoliciesApiClient _policyClient;
        private readonly ILogger _logger;

        public ReleaseManagementSpecificationService(IReleaseManagementRepository repo,
            IReleaseToChannelSqlMappingContext releaseToChannelSqlMappingContext,
            IPoliciesApiClient policiesApiClient,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(repo, nameof(repo));
            Guard.ArgumentNotNull(releaseToChannelSqlMappingContext, nameof(releaseToChannelSqlMappingContext));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _repo = repo;
            _releaseToChannelSqlMappingContext = releaseToChannelSqlMappingContext;
            _policyClient = policiesApiClient;
            _policyClientPolicy = publishingResiliencePolicies.PoliciesApiClient;
            _logger = logger;
        }

        public async Task EnsureReleaseManagementSpecification(SpecificationSummary specification)
        {
            string fundingStreamId = specification.FundingStreams.First().Id;
            string fundingPeriodId = specification.FundingPeriod.Id;

            SqlModels.FundingPeriod fundingPeriod = await _repo.GetFundingPeriodByCode(fundingPeriodId);
            if (fundingPeriod == null)
            {
                ApiResponse<IEnumerable<FundingPeriod>> policyFundingPeriods = await _policyClientPolicy.ExecuteAsync(() => _policyClient.GetFundingPeriods());
                FundingPeriod policyFundingPeriod = policyFundingPeriods.Content.SingleOrDefault(_ => _.Id == fundingPeriodId);
                fundingPeriod = new SqlModels.FundingPeriod()
                {
                    FundingPeriodCode = policyFundingPeriod.Id,
                    FundingPeriodName = policyFundingPeriod.Name,
                };

                fundingPeriod = await _repo.CreateFundingPeriodUsingAmbientTransaction(fundingPeriod);
            }

            SqlModels.FundingStream fundingStream = await _repo.GetFundingStreamByCode(fundingStreamId);
            if (fundingStream == null)
            {
                ApiResponse<IEnumerable<FundingStream>> policyFundingStreams = await _policyClientPolicy.ExecuteAsync(() => _policyClient.GetFundingStreams());
                FundingStream policyFundingStream = policyFundingStreams.Content.Single(_ => _.Id == fundingStreamId);
                fundingStream = new SqlModels.FundingStream()
                {
                    FundingStreamCode = policyFundingStream.Id,
                    FundingStreamName = policyFundingStream.Name,
                };

                fundingStream = await _repo.CreateFundingStreamUsingAmbientTransaction(fundingStream);
            }

            SqlModels.Specification existingSpecification = await _repo.GetSpecificationById(specification.Id);
            if (existingSpecification == null)
            {

                SqlModels.Specification newSpec = new SqlModels.Specification
                {
                    SpecificationId = specification.Id,
                    SpecificationName = specification.Name,
                    FundingStreamId = fundingStream.FundingStreamId,
                    FundingPeriodId = fundingPeriod.FundingPeriodId
                };

                existingSpecification = await _repo.CreateSpecificationUsingAmbientTransaction(newSpec);
            }
            else
            {
                if (!specification.Name.Equals(existingSpecification.SpecificationName))
                {
                    existingSpecification.SpecificationName = specification.Name;
                    await _repo.UpdateSpecificationUsingAmbientTransaction(existingSpecification);
                }
            }

            if (_releaseToChannelSqlMappingContext.Specification == null)
            {
                _releaseToChannelSqlMappingContext.Specification = existingSpecification;
            }
        }
    }
}
