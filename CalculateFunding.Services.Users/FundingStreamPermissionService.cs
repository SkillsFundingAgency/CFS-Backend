using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Health;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Users;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Users.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Users
{
    public class FundingStreamPermissionService : IFundingStreamPermissionService, IHealthChecker
    {
        private readonly IUserRepository _userRepository;
        private readonly ISpecificationRepository _specificationsRepository;
        private readonly IVersionRepository<FundingStreamPermissionVersion> _fundingStreamPermissionVersionRepository;
        private readonly ICacheProvider _cacheProvider;
        private readonly IMapper _mapper;

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
            IUsersResiliencePolicies policies)
        {
            Guard.ArgumentNotNull(userRepository, nameof(userRepository));
            Guard.ArgumentNotNull(specificationRepository, nameof(specificationRepository));
            Guard.ArgumentNotNull(fundingStreamPermissionVersionRepository, nameof(fundingStreamPermissionVersionRepository));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(policies, nameof(policies));

            _userRepository = userRepository;
            _specificationsRepository = specificationRepository;
            _fundingStreamPermissionVersionRepository = fundingStreamPermissionVersionRepository;
            _cacheProvider = cacheProvider;
            _mapper = mapper;

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

        public Task OnSpecificationUpdate(Message message)
        {
            return Task.CompletedTask;
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
                            CanPublishFunding = false
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
            };
        }

        private async Task ClearEffectivePermissionsForUser(string userId)
        {
            // Delete entire hash set for user (removing all effective permissions per spec)
            await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.DeleteHashSet($"{CacheKeys.EffectivePermissions}:{userId}"));
        }
    }
}
