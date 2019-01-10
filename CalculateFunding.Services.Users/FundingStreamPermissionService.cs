using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Exceptions;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Users;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Common.Caching;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Users.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Serilog;

namespace CalculateFunding.Services.Users
{
    public class FundingStreamPermissionService : IFundingStreamPermissionService, IHealthChecker
    {
        private readonly IUserRepository _userRepository;
        private readonly ISpecificationRepository _specificationsRepository;
        private readonly IVersionRepository<FundingStreamPermissionVersion> _fundingStreamPermissionVersionRepository;
        private readonly ICacheProvider _cacheProvider;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly Polly.Policy _userRepositoryPolicy;
        private readonly Polly.Policy _specificationsRepositoryPolicy;
        private readonly Polly.Policy _fundingStreamPermissionVersionRepositoryPolicy;
        private readonly Polly.Policy _cacheProviderPolicy;

        public FundingStreamPermissionService(
            IUserRepository userRepository,
            ISpecificationRepository specificationRepository,
            IVersionRepository<FundingStreamPermissionVersion> fundingStreamPermissionVersionRepository,
            ICacheProvider cacheProvider,
            IMapper mapper,
            ILogger logger,
            IUsersResiliencePolicies policies)
        {
            Guard.ArgumentNotNull(userRepository, nameof(userRepository));
            Guard.ArgumentNotNull(specificationRepository, nameof(specificationRepository));
            Guard.ArgumentNotNull(fundingStreamPermissionVersionRepository, nameof(fundingStreamPermissionVersionRepository));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(policies, nameof(policies));

            _userRepository = userRepository;
            _specificationsRepository = specificationRepository;
            _fundingStreamPermissionVersionRepository = fundingStreamPermissionVersionRepository;
            _cacheProvider = cacheProvider;
            _mapper = mapper;
            _logger = logger;

            _userRepositoryPolicy = policies.UserRepositoryPolicy;
            _specificationsRepositoryPolicy = policies.SpecificationRepositoryPolicy;
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

        public async Task<IActionResult> GetFundingStreamPermissionsForUser(string userId, HttpRequest request)
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

        public async Task<IActionResult> UpdatePermissionForUser(string userId, string fundingStreamId, HttpRequest request)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new BadRequestObjectResult($"{nameof(userId)} is empty or null");
            }

            if (string.IsNullOrWhiteSpace(fundingStreamId))
            {
                return new BadRequestObjectResult($"{nameof(fundingStreamId)} is empty or null");
            }

            Guard.ArgumentNotNull(request, nameof(request));

            User user = await _userRepositoryPolicy.ExecuteAsync(() => _userRepository.GetUserById(userId));
            if (user == null)
            {
                return new PreconditionFailedResult("userId not found");
            }

            string json = await request.GetRawBodyStringAsync();

            FundingStreamPermissionUpdateModel updateModel = JsonConvert.DeserializeObject<FundingStreamPermissionUpdateModel>(json);

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

                Reference author = request.GetUserOrDefault();

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

        public async Task<IActionResult> GetEffectivePermissionsForUser(string userId, string specificationId, HttpRequest request)
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
                SpecificationSummary specification = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specificationsRepository.GetSpecificationSummaryById(specificationId));
                if (specification == null)
                {
                    return new PreconditionFailedResult("Specification not found");
                }

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
                        permissionsForUser.Add(new FundingStreamPermission()
                        {
                            UserId = userId,
                            FundingStreamId = fundingStream.Id,
                            CanApproveFunding = false,
                            CanChooseFunding = false,
                            CanCreateSpecification = false,
                            CanEditCalculations = false,
                            CanEditSpecification = false,
                            CanMapDatasets = false,
                            CanPublishFunding = false,
                            CanAdministerFundingStream = false,
                            CanApproveSpecification = false,
                            CanCreateQaTests = false,
                            CanEditQaTests = false,
                            CanRefreshFunding = false,
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
                    CanPublishFunding = false,
                    CanRefreshFunding = false,
                    CanEditQaTests = false,
                    CanCreateQaTests = false,
                    CanApproveSpecification = false,
                    CanAdministerFundingStream = false,
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
                CanPublishFunding = permissionsForUser.All(p => p.CanPublishFunding),
                CanAdministerFundingStream = permissionsForUser.All(p => p.CanAdministerFundingStream),
                CanApproveSpecification = permissionsForUser.All(p => p.CanApproveSpecification),
                CanCreateQaTests = permissionsForUser.All(p => p.CanCreateQaTests),
                CanEditQaTests = permissionsForUser.All(p => p.CanEditQaTests),
                CanRefreshFunding = permissionsForUser.All(p => p.CanRefreshFunding),
            };
        }

        private async Task ClearEffectivePermissionsForUser(string userId)
        {
            // Delete entire hash set for user (removing all effective permissions per spec)
            await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.DeleteHashSet($"{CacheKeys.EffectivePermissions}:{userId}"));
        }
    }
}
