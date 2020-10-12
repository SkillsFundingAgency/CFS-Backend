using AutoMapper;
using CalculateFunding.Common.ApiClient.FundingDataZone;
using CalculateFunding.Common.ApiClient.FundingDataZone.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Providers.ViewModels;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Core.Constants;

namespace CalculateFunding.Services.Providers
{
    public class ProviderSnapshotDataLoadService : IProviderSnapshotDataLoadService
    {
        private const string SpecificationIdKey = "specification-id";
        private const string FundingStreamIdKey = "fundingstream-id";
        private const string ProviderSnapshotIdKey = "providerSanpshot-id";
        private const string JobIdKey = "jobId";
        private readonly ILogger _logger;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly IProviderVersionService _providerVersionService;
        private readonly AsyncPolicy _specificationsApiClientPolicy;
        private readonly IFundingDataZoneApiClient _fundingDataZoneApiClient;
        private readonly IMapper _mapper;
        private readonly IJobManagement _jobManagement;
        private readonly AsyncPolicy _fundingDataZoneApiClientPolicy;

        public ProviderSnapshotDataLoadService(ILogger logger,
            ISpecificationsApiClient specificationsApiClient,
            IProviderVersionService providerVersionService,
            IProvidersResiliencePolicies resiliencePolicies,
            IFundingDataZoneApiClient fundingDataZoneApiClient,
            IMapper mapper,
            IJobManagement jobManagement)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(providerVersionService, nameof(providerVersionService));
            Guard.ArgumentNotNull(resiliencePolicies?.SpecificationsApiClient, nameof(resiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.FundingDataZoneApiClient, nameof(resiliencePolicies.FundingDataZoneApiClient));
            Guard.ArgumentNotNull(fundingDataZoneApiClient, nameof(fundingDataZoneApiClient));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));

            _logger = logger;
            _specificationsApiClient = specificationsApiClient;
            _providerVersionService = providerVersionService;
            _specificationsApiClientPolicy = resiliencePolicies.SpecificationsApiClient;
            _fundingDataZoneApiClientPolicy = resiliencePolicies.FundingDataZoneApiClient;
            _fundingDataZoneApiClient = fundingDataZoneApiClient;
            _mapper = mapper;
            _jobManagement = jobManagement;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth providerVersionServiceHealth = await _providerVersionService.IsHealthOk();

            ServiceHealth health = new ServiceHealth
            {
                Name = nameof(ProviderSnapshotDataLoadService)
            };
            health.Dependencies.AddRange(providerVersionServiceHealth.Dependencies);
            
            return health;
        }

        public async Task LoadProviderSnapshotData(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            string jobId = message.GetUserProperty<string>(JobIdKey);
            string specificationId = message.GetUserProperty<string>(SpecificationIdKey);
            string fundingStreamId = message.GetUserProperty<string>(FundingStreamIdKey);
            string providerSnapshotIdValue = message.GetUserProperty<string>(ProviderSnapshotIdKey);
            Reference user = message.GetUserDetails();
            string correlationId = message.GetCorrelationId();

            if (string.IsNullOrWhiteSpace(providerSnapshotIdValue) || !int.TryParse(providerSnapshotIdValue, out int providerSnapshotId))
            {
                throw new Exception($"Invalid provider snapshot id");
            }

            ProviderSnapshot providerSnapshot = await GetProviderSnapshot(fundingStreamId, providerSnapshotId);

            string providerVersionId = $"{fundingStreamId}-{providerSnapshot.TargetDate:yyyy}-{providerSnapshot.TargetDate:MM}-{providerSnapshot.TargetDate:dd}-{providerSnapshotId}";
            bool isProviderVersionExists = await _providerVersionService.Exists(providerVersionId);

            if (!isProviderVersionExists)
            {
                IEnumerable<Common.ApiClient.FundingDataZone.Models.Provider> fdzProviders = await GetProvidersInSnapshot(providerSnapshotId);

                ProviderVersionViewModel providerVersionViewModel = CreateProviderVersionViewModel(fundingStreamId, providerVersionId, providerSnapshot, fdzProviders);
               (bool success, IActionResult actionResult) = await _providerVersionService.UploadProviderVersion(providerVersionId, providerVersionViewModel);

                if(!success)
                {
                    string errorMessage = $"Failed to upload provider version {providerVersionId}. {GetErrorMessage(actionResult, providerVersionId)}";
                    
                    _logger.Error(errorMessage);
                    
                    throw new Exception(errorMessage);
                }
            }

            HttpStatusCode httpStatusCode = await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.SetProviderVersion(specificationId, providerVersionId));

            if(!httpStatusCode.IsSuccess())
            {
                string errorMessage = $"Unable to update the specification - {specificationId}, with provider version id  - {providerVersionId}. HttpStatusCode - {httpStatusCode}";
                
                _logger.Error(errorMessage);
                
                throw new Exception(errorMessage);
            }

            JobCreateModel mapFdzDatasetsJobCreateModel = new JobCreateModel
            {
                Trigger = new Trigger
                {
                    EntityId = specificationId,
                    EntityType = "Specification",
                    Message = "Map datasets for all relationships in specification"
                },
                InvokerUserId = user.Id,
                InvokerUserDisplayName = user.Name,
                JobDefinitionId = JobConstants.DefinitionNames.MapFdzDatasetsJob,
                ParentJobId = null,
                SpecificationId = specificationId,
                CorrelationId = correlationId,
                Properties = new Dictionary<string, string>
                    {
                        {"specification-id", specificationId}
                    }
            };

            try
            {
                await _jobManagement.QueueJob(mapFdzDatasetsJobCreateModel);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to queue MapFdzDatasetsJob for specification - {specificationId}";
                _logger.Error(ex, errorMessage);
                throw;
            }
        }

        private ProviderVersionViewModel CreateProviderVersionViewModel(string fundingStreamId, string providerVersionId, ProviderSnapshot providerSnapshot, IEnumerable<Common.ApiClient.FundingDataZone.Models.Provider> fdzProviders)
        {
            IEnumerable<Models.Providers.Provider> providers = fdzProviders.Select(_ =>
            {
                return _mapper.Map<Common.ApiClient.FundingDataZone.Models.Provider, Models.Providers.Provider>(_, opt =>
                    opt.AfterMap((src, dest) =>
                    {
                        dest.ProviderVersionId = providerVersionId;
                        dest.ProviderVersionIdProviderId = $"{providerVersionId}_{dest.ProviderId}";
                    }));
            });

            ProviderVersionViewModel providerVersionViewModel = new ProviderVersionViewModel()
            {
                FundingStream = fundingStreamId,
                ProviderVersionId = providerVersionId,
                VersionType = ProviderVersionType.SystemImported,
                TargetDate = providerSnapshot.TargetDate,
                Created = DateTimeOffset.Now,
                Name = providerSnapshot.Name,
                Description = providerSnapshot.Description,
                Version = 1,
                Providers = providers
            };

            return providerVersionViewModel;
        }

        private string GetErrorMessage(IActionResult actionResult, string providerVersionId)
        {
            return actionResult switch
            {
                ConflictResult _ => $"ProviderVersion alreay exists for - {providerVersionId}",
                BadRequestObjectResult r => $"Validation Errors - {r.AsJson()}",
                _ => string.Empty,
            };
        }

        private async Task<ProviderSnapshot> GetProviderSnapshot(string fundingStreamId, int providerSnapshotId)
        {
            ApiResponse<IEnumerable<ProviderSnapshot>> fundingStreamProviderSnapshotsResponse = await _fundingDataZoneApiClientPolicy.ExecuteAsync(
                            () => _fundingDataZoneApiClient.GetProviderSnapshotsForFundingStream(fundingStreamId));

            if (!fundingStreamProviderSnapshotsResponse.StatusCode.IsSuccess())
            {
                string errorMessage = $"Unable to retrieve FDZ ProviderSnapshots for funding stream -{fundingStreamId}";
                _logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }

            ProviderSnapshot providerSnapshot = fundingStreamProviderSnapshotsResponse.Content.FirstOrDefault(x => x.ProviderSnapshotId == providerSnapshotId);

            if (providerSnapshot == null)
            {
                string errorMessage = $"FDZ ProviderSnapshot not found for funding stream - {fundingStreamId} and provider snapshot id - {providerSnapshotId}";
                _logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }

            return providerSnapshot;
        }

        private async Task<IEnumerable<Common.ApiClient.FundingDataZone.Models.Provider>> GetProvidersInSnapshot(int providerSnapshotId)
        {
            ApiResponse<IEnumerable<Common.ApiClient.FundingDataZone.Models.Provider>> providersInSnapshotResponse = await _fundingDataZoneApiClientPolicy.ExecuteAsync(
                            () => _fundingDataZoneApiClient.GetProvidersInSnapshot(providerSnapshotId));


            if (!providersInSnapshotResponse.StatusCode.IsSuccess())
            {
                string errorMessage = $"Unable to retrieve providers for ProviderSnapshotId -{providerSnapshotId}";
                _logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }

            return providersInSnapshotResponse.Content;
        }
    }
}
