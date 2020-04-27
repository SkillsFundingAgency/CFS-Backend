using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Messages;
using CalculateFunding.Models.Users;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Users.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Serilog;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;

namespace CalculateFunding.Services.Users
{
    public class FundingStreamPermissionService : IFundingStreamPermissionService, IHealthChecker
    {
        private readonly IUserRepository _userRepository;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly IVersionRepository<FundingStreamPermissionVersion> _fundingStreamPermissionVersionRepository;
        private readonly ICacheProvider _cacheProvider;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly Polly.AsyncPolicy _userRepositoryPolicy;
        private readonly Polly.AsyncPolicy _specificationsApiClientPolicy;
        private readonly Polly.AsyncPolicy _fundingStreamPermissionVersionRepositoryPolicy;
        private readonly Polly.AsyncPolicy _cacheProviderPolicy;

        public FundingStreamPermissionService(
            IUserRepository userRepository,
            ISpecificationsApiClient specificationsApiClient,
            IVersionRepository<FundingStreamPermissionVersion> fundingStreamPermissionVersionRepository,
            ICacheProvider cacheProvider,
            IMapper mapper,
            ILogger logger,
            IUsersResiliencePolicies policies)
        {
            Guard.ArgumentNotNull(userRepository, nameof(userRepository));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(fundingStreamPermissionVersionRepository, nameof(fundingStreamPermissionVersionRepository));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(policies?.UserRepositoryPolicy, nameof(policies.UserRepositoryPolicy));
            Guard.ArgumentNotNull(policies?.SpecificationApiClient, nameof(policies.SpecificationApiClient));
            Guard.ArgumentNotNull(policies?.FundingStreamPermissionVersionRepositoryPolicy, nameof(policies.FundingStreamPermissionVersionRepositoryPolicy));
            Guard.ArgumentNotNull(policies?.CacheProviderPolicy, nameof(policies.CacheProviderPolicy));

            _userRepository = userRepository;
            _specificationsApiClient = specificationsApiClient;
            _fundingStreamPermissionVersionRepository = fundingStreamPermissionVersionRepository;
            _cacheProvider = cacheProvider;
            _mapper = mapper;
            _logger = logger;

            _userRepositoryPolicy = policies.UserRepositoryPolicy;
            _specificationsApiClientPolicy = policies.SpecificationApiClient;
            _fundingStreamPermissionVersionRepositoryPolicy = policies.FundingStreamPermissionVersionRepositoryPolicy;
            _cacheProviderPolicy = policies.CacheProviderPolicy;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth userRepoHealth = await ((IHealthChecker)_userRepository).IsHealthOk();
            (bool Ok, string Message) cacheHealth = await _cacheProvider.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(FundingStreamPermissionService)
            };
            health.Dependencies.AddRange(userRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = cacheHealth.Ok, DependencyName = _cacheProvider.GetType().GetFriendlyName(), Message = cacheHealth.Message });

            return health;
        }

        public async Task<IActionResult> GetFundingStreamPermissionsForUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new BadRequestObjectResult($"{nameof(userId)} is null or empty");
            }

            List<FundingStreamPermissionCurrent> results = new List<FundingStreamPermissionCurrent>();

            IEnumerable<FundingStreamPermission> permissions = await _userRepositoryPolicy.ExecuteAsync(() => _userRepository.GetFundingStreamPermissions(userId));

            if (permissions.AnyWithNullCheck())
            {
                foreach (FundingStreamPermission permission in permissions)
                {
                    results.Add(_mapper.Map<FundingStreamPermissionCurrent>(permission));
                }
            }

            return new OkObjectResult(results);
        }

        public async Task<IActionResult> UpdatePermissionForUser(string userId, string fundingStreamId, FundingStreamPermissionUpdateModel updateModel, Reference author)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new BadRequestObjectResult($"{nameof(userId)} is empty or null");
            }

            if (string.IsNullOrWhiteSpace(fundingStreamId))
            {
                return new BadRequestObjectResult($"{nameof(fundingStreamId)} is empty or null");
            }

            User user = await _userRepositoryPolicy.ExecuteAsync(() => _userRepository.GetUserById(userId));
            if (user == null)
            {
                return new PreconditionFailedResult("userId not found");
            }

            FundingStreamPermission existingPermissions = await _userRepositoryPolicy.ExecuteAsync(() => _userRepository.GetFundingStreamPermission(userId, fundingStreamId));

            FundingStreamPermission newPermissions = _mapper.Map<FundingStreamPermissionUpdateModel, FundingStreamPermission>(updateModel);
            newPermissions.FundingStreamId = fundingStreamId;
            newPermissions.UserId = userId;

            if (existingPermissions == null || !existingPermissions.HasSamePermissions(newPermissions))
            {
                HttpStatusCode saveResult = await _userRepositoryPolicy.ExecuteAsync(() => _userRepository.UpdateFundingStreamPermission(newPermissions));
                if (saveResult != HttpStatusCode.OK && saveResult != HttpStatusCode.Created)
                {
                    return new InternalServerErrorResult($"Saving funding stream permission to repository returned '{saveResult}'");
                }

                FundingStreamPermissionVersion version = new FundingStreamPermissionVersion()
                {
                    Author = author,
                    Permission = newPermissions,
                    PublishStatus = Models.Versioning.PublishStatus.Updated,
                    Date = DateTimeOffset.Now,
                    UserId = userId,
                };

                version.Version = await _fundingStreamPermissionVersionRepositoryPolicy.ExecuteAsync(() => _fundingStreamPermissionVersionRepository.GetNextVersionNumber(version, partitionKeyId: userId));

                await _fundingStreamPermissionVersionRepositoryPolicy.ExecuteAsync(() => _fundingStreamPermissionVersionRepository.SaveVersion(version, userId));

                await ClearEffectivePermissionsForUser(userId);
            }

            return new OkObjectResult(_mapper.Map<FundingStreamPermissionCurrent>(newPermissions));
        }

        public async Task OnSpecificationUpdate(Message message)
        {
            SpecificationVersionComparisonModel versionComparison = message.GetPayloadAsInstanceOf<SpecificationVersionComparisonModel>();

            if (versionComparison == null || versionComparison.Current == null || versionComparison.Previous == null)
            {
                _logger.Error($"A null versionComparison was provided to users {nameof(OnSpecificationUpdate)}");

                throw new InvalidModelException(nameof(SpecificationVersionComparisonModel), new[] { "Null or invalid model provided" });
            }

            string specificationId = versionComparison.Id;
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error($"A null specificationId was provided to users {nameof(OnSpecificationUpdate)} in model");

                throw new InvalidModelException(nameof(SpecificationVersionComparisonModel), new[] { "Null or invalid specificationId on model" });
            }

            IEnumerable<string> previousFundingStreams = versionComparison.Previous.FundingStreams.OrderBy(c => c.Id).Select(f => f.Id);
            IEnumerable<string> currentFundingStreams = versionComparison.Current.FundingStreams.OrderBy(c => c.Id).Select(f => f.Id);

            if (!previousFundingStreams.SequenceEqual(currentFundingStreams))
            {
                _logger.Information("Found changed funding streams for specification '{SpecificationId}' Previous: {PreviousFundingStreams} Current {CurrentFundingStreams}", specificationId, previousFundingStreams, currentFundingStreams);

                Dictionary<string, bool> userIds = new Dictionary<string, bool>();

                IEnumerable<string> allFundingStreamIds = previousFundingStreams.Union(currentFundingStreams);

                foreach (string fundingStreamId in allFundingStreamIds)
                {
                    IEnumerable<FundingStreamPermission> userPermissions = await _userRepositoryPolicy.ExecuteAsync(() => _userRepository.GetUsersWithFundingStreamPermissions(fundingStreamId));
                    foreach (FundingStreamPermission permission in userPermissions)
                    {
                        if (!userIds.ContainsKey(permission.UserId))
                        {
                            userIds.Add(permission.UserId, true);
                        }
                    }
                }

                foreach (string userId in userIds.Keys)
                {
                    _logger.Information("Clearing effective permissions for userId '{UserId}' for specification '{SpecificationId}'", userId, specificationId);
                    await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.DeleteHashKey<EffectiveSpecificationPermission>($"{CacheKeys.EffectivePermissions}:{userId}", specificationId));
                }
            }
            else
            {
                _logger.Information("No funding streams have changed for specification '{SpecificationId}' which require effective permission clearing.", specificationId);
            }
        }

        public async Task<IActionResult> GetEffectivePermissionsForUser(string userId, string specificationId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new BadRequestObjectResult($"{nameof(userId)} is empty or null");
            }

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                return new BadRequestObjectResult($"{nameof(specificationId)} is empty or null");
            }

            EffectiveSpecificationPermission cachedPermissions = await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.GetHashValue<EffectiveSpecificationPermission>($"{CacheKeys.EffectivePermissions}:{userId}", specificationId));
            if (cachedPermissions != null)
            {
                return new OkObjectResult(cachedPermissions);
            }
            else
            {
                ApiResponse<SpecModel.SpecificationSummary> specificationApiResponse =
                    await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(specificationId));

                if (!specificationApiResponse.StatusCode.IsSuccess() || specificationApiResponse.Content == null)
                {
                    return new PreconditionFailedResult("Specification not found");
                }

                SpecModel.SpecificationSummary specification = specificationApiResponse.Content;

                List<FundingStreamPermission> permissionsForUser = new List<FundingStreamPermission>();
                foreach (Reference fundingStream in specification.FundingStreams)
                {
                    FundingStreamPermission permission = await _userRepositoryPolicy.ExecuteAsync(() => _userRepository.GetFundingStreamPermission(userId, fundingStream.Id));
                    if (permission != null)
                    {
                        permissionsForUser.Add(permission);
                    }
                    else
                    {
                        // Add permission for this funding stream with no permissions - used further down to calculate permissions (required for pessimistic permissions)
                        permissionsForUser.Add(new FundingStreamPermission
                        {
                            UserId = userId,
                            FundingStreamId = fundingStream.Id,
                            CanApproveFunding = false,
                            CanChooseFunding = false,
                            CanCreateSpecification = false,
                            CanEditCalculations = false,
                            CanEditSpecification = false,
                            CanMapDatasets = false,
                            CanReleaseFunding = false,
                            CanAdministerFundingStream = false,
                            CanApproveSpecification = false,
                            CanCreateQaTests = false,
                            CanDeleteCalculations = false,
                            CanDeleteSpecification = false,
                            CanDeleteQaTests = false,
                            CanEditQaTests = false,
                            CanRefreshFunding = false,
                            CanCreateTemplates = false,
                            CanEditTemplates = false,
                            CanDeleteTemplates = false,
                            CanApproveTemplates = false
                        });
                    }
                }

                EffectiveSpecificationPermission specificationPermissions = GeneratePermissions(permissionsForUser, specificationId, userId);

                string userPermissionHashKey = $"{CacheKeys.EffectivePermissions}:{userId}";

                // Does the hash set for this user already exist - used to determine the timeout for the hash set below
                bool existingHashSetExists = await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.HashSetExists(userPermissionHashKey));

                // Cache effective permissions for the specification / user
                await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.SetHashValue(userPermissionHashKey, specificationId, specificationPermissions));

                // If the hash set does not exist, then set an expiry for the whole hash set. This stops the users permissions being stored indefinitely
                if (!existingHashSetExists)
                {
                    await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.SetHashExpiry(userPermissionHashKey, DateTime.UtcNow.AddHours(12)));
                }

                return new OkObjectResult(specificationPermissions);
            }
        }

        private EffectiveSpecificationPermission GeneratePermissions(IEnumerable<FundingStreamPermission> permissionsForUser, string specificationId, string userId)
        {
            if (!permissionsForUser.AnyWithNullCheck())
            {
                // Set effective permissions to nothing
                return new EffectiveSpecificationPermission()
                {
                    SpecificationId = specificationId,
                    UserId = userId,
                    CanApproveFunding = false,
                    CanChooseFunding = false,
                    CanCreateSpecification = false,
                    CanEditCalculations = false,
                    CanEditSpecification = false,
                    CanMapDatasets = false,
                    CanReleaseFunding = false,
                    CanRefreshFunding = false,
                    CanEditQaTests = false,
                    CanCreateQaTests = false,
                    CanApproveSpecification = false,
                    CanAdministerFundingStream = false,
                    CanDeleteSpecification = false,
                    CanDeleteCalculations = false,
                    CanDeleteQaTests = false,
                    CanCreateTemplates = false,
                    CanEditTemplates = false,
                    CanDeleteTemplates = false,
                    CanApproveTemplates = false
                };
            }

            // Require all funding streams to have the permission set to grant the individual permission
            return new EffectiveSpecificationPermission()
            {
                SpecificationId = specificationId,
                UserId = userId,
                CanApproveFunding = permissionsForUser.All(p => p.CanApproveFunding),
                CanChooseFunding = permissionsForUser.All(p => p.CanChooseFunding),
                CanCreateSpecification = permissionsForUser.All(p => p.CanCreateSpecification),
                CanEditCalculations = permissionsForUser.All(p => p.CanEditCalculations),
                CanEditSpecification = permissionsForUser.All(p => p.CanEditSpecification),
                CanMapDatasets = permissionsForUser.All(p => p.CanMapDatasets),
                CanReleaseFunding = permissionsForUser.All(p => p.CanReleaseFunding),
                CanAdministerFundingStream = permissionsForUser.All(p => p.CanAdministerFundingStream),
                CanApproveSpecification = permissionsForUser.All(p => p.CanApproveSpecification),
                CanCreateQaTests = permissionsForUser.All(p => p.CanCreateQaTests),
                CanEditQaTests = permissionsForUser.All(p => p.CanEditQaTests),
                CanRefreshFunding = permissionsForUser.All(p => p.CanRefreshFunding),
                CanDeleteSpecification = permissionsForUser.All(p => p.CanDeleteSpecification),
                CanDeleteCalculations = permissionsForUser.All(p => p.CanDeleteCalculations),
                CanDeleteQaTests = permissionsForUser.All(p => p.CanDeleteQaTests),
                CanCreateTemplates = permissionsForUser.All(p => p.CanCreateTemplates),
                CanEditTemplates = permissionsForUser.All(p => p.CanEditTemplates),
                CanDeleteTemplates = permissionsForUser.All(p => p.CanDeleteTemplates),
                CanApproveTemplates = permissionsForUser.All(p => p.CanApproveTemplates)
            };
        }

        private async Task ClearEffectivePermissionsForUser(string userId)
        {
            // Delete entire hash set for user (removing all effective permissions per spec)
            await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.DeleteHashSet($"{CacheKeys.EffectivePermissions}:{userId}"));
        }
    }
}
