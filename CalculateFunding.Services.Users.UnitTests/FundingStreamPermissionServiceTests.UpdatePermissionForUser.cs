using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Users;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Common.Caching;
using CalculateFunding.Services.Users.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Users
{
    public partial class FundingStreamPermissionServiceTests
    {
        [TestMethod]
        public async Task UpdatePermissionForUser_WhenUserIdIsEmpty_ThenBadRequestReturned()
        {
            // Arrange
            FundingStreamPermissionService service = CreateService();

            // Act
            IActionResult result = await service.UpdatePermissionForUser(null, FundingStreamId, null, null);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("userId is empty or null");
        }

        [TestMethod]
        public async Task UpdatePermissionForUser_WhenFundingStreamIdIsEmpty_ThenBadRequestReturned()
        {
            // Arrange
            FundingStreamPermissionService service = CreateService();
            string fundingStreamId = null;

            // Act
            IActionResult result = await service.UpdatePermissionForUser(UserId, fundingStreamId, null, null);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("fundingStreamId is empty or null");
        }

        [TestMethod]
        public async Task UpdatePermissionForUser_WhenUserNotFound_ThenPreconditionFailedReturned()
        {
            // Arrange
            IUserRepository userRepository = CreateUserRepository();
            User user = null;

            userRepository
                .GetUserById(Arg.Is(UserId))
                .Returns(user);

            FundingStreamPermissionService service = CreateService(userRepository);

            // Act
            IActionResult result = await service.UpdatePermissionForUser(UserId, FundingStreamId, null, null);

            // Assert
            result
                .Should()
                .BeOfType<PreconditionFailedResult>()
                .Which
                .Value
                .Should()
                .Be("userId not found");
        }

        [TestMethod]
        public async Task UpdatePermissionForUser_WhenPermissionsAreSetOnAFundingStreamAndNoneHaveBeenSetBefore_ThenPermissionsSaved()
        {
            // Arrange
            IUserRepository userRepository = CreateUserRepository();
            User user = new User()
            {
                UserId = UserId
            };

            userRepository
                .GetUserById(Arg.Is(UserId))
                .Returns(user);

            FundingStreamPermission existingPermission = null;

            userRepository
                .GetFundingStreamPermission(Arg.Is(UserId), Arg.Is(FundingStreamId))
                .Returns(existingPermission);

            userRepository
                .UpdateFundingStreamPermission(Arg.Any<FundingStreamPermission>())
                .Returns(HttpStatusCode.Created);

            ICacheProvider cacheProvider = CreateCacheProvider();

            FundingStreamPermissionUpdateModel updateModel = new FundingStreamPermissionUpdateModel()
            {
                CanApproveFunding = true,
                CanChooseFunding = false,
                CanCreateSpecification = false,
                CanEditCalculations = false,
                CanEditSpecification = false,
                CanMapDatasets = false,
                CanReleaseFunding = false,
                CanReleaseFundingForStatement = false,
                CanReleaseFundingForPaymentOrContract = false,
                CanCreateTemplates = true,
                CanEditTemplates = true,
                CanApproveTemplates = true,
                CanCreateProfilePattern = true,
                CanEditProfilePattern = true,
                CanAssignProfilePattern = false,
                CanApplyCustomProfilePattern = false,
                CanApproveCalculations = true,
                CanApproveAnyCalculations = false,
                CanApproveAllCalculations = false
            };
            
            IVersionRepository<FundingStreamPermissionVersion> versionRepository = CreateFundingStreamPermissionRepository();

            versionRepository
                .GetNextVersionNumber(Arg.Any<FundingStreamPermissionVersion>(), 0, Arg.Is(UserId))
                .Returns(1);

            FundingStreamPermissionService service = CreateService(
                userRepository,
                cacheProvider: cacheProvider,
                fundingStreamPermissionVersionRepository: versionRepository);

            // Act
            IActionResult result = await service.UpdatePermissionForUser(UserId, FundingStreamId, updateModel, null);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeEquivalentTo<FundingStreamPermissionCurrent>(new FundingStreamPermissionCurrent()
                {
                    UserId = UserId,
                    FundingStreamId = FundingStreamId,
                    CanApproveFunding = true,
                    CanChooseFunding = false,
                    CanCreateSpecification = false,
                    CanEditCalculations = false,
                    CanEditSpecification = false,
                    CanMapDatasets = false,
                    CanReleaseFunding = false,
                    CanReleaseFundingForStatement = false,
                    CanReleaseFundingForPaymentOrContract = false,
                    CanCreateTemplates = true,
                    CanEditTemplates = true,
                    CanApproveTemplates = true,
                    CanCreateProfilePattern = true,
                    CanEditProfilePattern = true,
                    CanAssignProfilePattern = false,
                    CanApplyCustomProfilePattern = false,
                    CanApproveCalculations = true,
                    CanApproveAnyCalculations = false,
                    CanApproveAllCalculations = false,
                });

            await userRepository
                .Received(1)
                .UpdateFundingStreamPermission(Arg.Is<FundingStreamPermission>(p =>
                    p.FundingStreamId == FundingStreamId &&
                    p.UserId == UserId &&
                    p.CanApproveFunding &&
                    !p.CanChooseFunding &&
                    !p.CanCreateSpecification &&
                    !p.CanEditCalculations &&
                    !p.CanEditSpecification &&
                    !p.CanMapDatasets &&
                    !p.CanReleaseFunding &&
                    !p.CanReleaseFundingForStatement &&
                    !p.CanReleaseFundingForPaymentOrContract &&
                    p.CanCreateTemplates &&
                    p.CanEditTemplates &&
                    p.CanApproveTemplates &&
                    p.CanCreateProfilePattern &&
                    p.CanEditProfilePattern &&
                    !p.CanAssignProfilePattern &&
                    !p.CanApplyCustomProfilePattern &&
                    p.CanApproveCalculations &&
                    !p.CanApproveAnyCalculations &&
                    !p.CanApproveAllCalculations
                ));

            await cacheProvider
                .Received(1)
                .DeleteHashSet(Arg.Is($"{CacheKeys.EffectivePermissions}:{UserId}"));

            await versionRepository
                .Received(1)
                .GetNextVersionNumber(Arg.Is<FundingStreamPermissionVersion>(v => v.EntityId == $"{UserId}_{FundingStreamId}"), partitionKeyId: Arg.Is(UserId));

            await versionRepository
                .Received(1)
                .SaveVersion(Arg.Is<FundingStreamPermissionVersion>(v =>
                    v.EntityId == $"{UserId}_{FundingStreamId}" &&
                    v.Version == 1
                ), Arg.Is(UserId));
        }

        [TestMethod]
        public async Task UpdatePermissionForUser_WhenPermissionsAreSetOnAFundingStreamAndUpdateExistingPermissionsInRepository_ThenPermissionsSaved()
        {
            // Arrange
            IUserRepository userRepository = CreateUserRepository();
            User user = new User()
            {
                UserId = UserId
            };

            userRepository
                .GetUserById(Arg.Is(UserId))
                .Returns(user);

            FundingStreamPermission existingPermission = new FundingStreamPermission()
            {
                UserId = UserId,
                FundingStreamId = FundingStreamId,
                CanApproveFunding = true,
                CanChooseFunding = false,
                CanCreateSpecification = false,
                CanEditCalculations = false,
                CanEditSpecification = false,
                CanMapDatasets = false,
                CanReleaseFunding = false,
                CanReleaseFundingForStatement = false,
                CanReleaseFundingForPaymentOrContract = false,
                CanCreateProfilePattern = false,
                CanEditProfilePattern = false,
                CanAssignProfilePattern = false,
                CanApplyCustomProfilePattern = false,
                CanApproveCalculations = true,
                CanApproveAnyCalculations = false,
                CanApproveAllCalculations = false
            };

            userRepository
                .GetFundingStreamPermission(Arg.Is(UserId), Arg.Is(FundingStreamId))
                .Returns(existingPermission);

            userRepository
                .UpdateFundingStreamPermission(Arg.Any<FundingStreamPermission>())
                .Returns(HttpStatusCode.Created);

            ICacheProvider cacheProvider = CreateCacheProvider();

            FundingStreamPermissionUpdateModel updateModel = new FundingStreamPermissionUpdateModel()
            {
                CanApproveFunding = true,
                CanChooseFunding = true,
                CanCreateSpecification = false,
                CanEditCalculations = false,
                CanEditSpecification = false,
                CanMapDatasets = false,
                CanReleaseFunding = false,
                CanReleaseFundingForStatement = false,
                CanReleaseFundingForPaymentOrContract = false,
                CanApproveTemplates = true,
                CanCreateProfilePattern = false,
                CanEditProfilePattern = false,
                CanAssignProfilePattern = false,
                CanApplyCustomProfilePattern = false,
                CanApproveCalculations = false,
                CanApproveAnyCalculations = true,
                CanApproveAllCalculations = true
            };

            IVersionRepository<FundingStreamPermissionVersion> versionRepository = CreateFundingStreamPermissionRepository();

            versionRepository
                .GetNextVersionNumber(Arg.Any<FundingStreamPermissionVersion>(), 0, Arg.Is(UserId))
                .Returns(2);

            FundingStreamPermissionService service = CreateService(
                userRepository,
                cacheProvider: cacheProvider,
                fundingStreamPermissionVersionRepository: versionRepository);

            // Act
            IActionResult result = await service.UpdatePermissionForUser(UserId, FundingStreamId, updateModel, null);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeEquivalentTo<FundingStreamPermissionCurrent>(new FundingStreamPermissionCurrent()
                {
                    UserId = UserId,
                    FundingStreamId = FundingStreamId,
                    CanApproveFunding = true,
                    CanChooseFunding = true,
                    CanCreateSpecification = false,
                    CanEditCalculations = false,
                    CanEditSpecification = false,
                    CanMapDatasets = false,
                    CanReleaseFunding = false,
                    CanReleaseFundingForStatement = false,
                    CanReleaseFundingForPaymentOrContract = false,
                    CanApproveTemplates = true,
                    CanCreateProfilePattern = false,
                    CanEditProfilePattern = false,
                    CanAssignProfilePattern = false,
                    CanApplyCustomProfilePattern = false,
                    CanApproveCalculations = false,
                    CanApproveAnyCalculations = true,
                    CanApproveAllCalculations = true
                });

            await userRepository
               .Received(1)
               .UpdateFundingStreamPermission(Arg.Is<FundingStreamPermission>(p =>
                   p.FundingStreamId == FundingStreamId &&
                   p.UserId == UserId &&
                   p.CanApproveFunding &&
                   p.CanChooseFunding &&
                   !p.CanCreateSpecification &&
                   !p.CanEditCalculations &&
                   !p.CanEditSpecification &&
                   !p.CanMapDatasets &&
                   !p.CanReleaseFunding &&
                   !p.CanReleaseFundingForStatement &&
                   !p.CanReleaseFundingForPaymentOrContract &&
                   !p.CanCreateTemplates &&
                   !p.CanEditTemplates &&
                   p.CanApproveTemplates &&
                   !p.CanCreateProfilePattern &&
                   !p.CanEditProfilePattern &&
                   !p.CanAssignProfilePattern &&
                   !p.CanApplyCustomProfilePattern && 
                   !p.CanApproveCalculations &&
                   p.CanApproveAnyCalculations &&
                   p.CanApproveAllCalculations
               ));

            await cacheProvider
                .Received(1)
                .DeleteHashSet(Arg.Is($"{CacheKeys.EffectivePermissions}:{UserId}"));

            await versionRepository
               .Received(1)
               .SaveVersion(Arg.Is<FundingStreamPermissionVersion>(v =>
                   v.EntityId == $"{UserId}_{FundingStreamId}" &&
                   v.Id == $"{UserId}_{FundingStreamId}_version_2" &&
                   v.Version == 2
               ), Arg.Is(UserId));
        }

        [TestMethod]
        public async Task UpdatePermissionForUser_WhenPermissionsAreSetOnAFundingStreamAndUpdateExistingPermissionsInRepositoryButNoPermissionsChanged_ThenPermissionsNotSavedToRepository()
        {
            // Arrange
            IUserRepository userRepository = CreateUserRepository();
            User user = new User()
            {
                UserId = UserId
            };

            userRepository
                .GetUserById(Arg.Is(UserId))
                .Returns(user);

            FundingStreamPermission existingPermission = new FundingStreamPermission()
            {
                UserId = UserId,
                FundingStreamId = FundingStreamId,
                CanApproveFunding = true,
                CanChooseFunding = true,
                CanCreateSpecification = false,
                CanEditCalculations = false,
                CanEditSpecification = false,
                CanMapDatasets = false,
                CanReleaseFunding = false,
                CanReleaseFundingForStatement = false,
                CanReleaseFundingForPaymentOrContract = false,
                CanApproveTemplates = true,
                CanCreateProfilePattern = false,
                CanEditProfilePattern = false,
                CanAssignProfilePattern = false,
                CanApplyCustomProfilePattern = false,
                CanApproveCalculations = false,
                CanApproveAnyCalculations = true,
                CanApproveAllCalculations = true
            };

            userRepository
                .GetFundingStreamPermission(Arg.Is(UserId), Arg.Is(FundingStreamId), Arg.Is(true))
                .Returns(existingPermission);

            userRepository
                .UpdateFundingStreamPermission(Arg.Any<FundingStreamPermission>())
                .Returns(HttpStatusCode.Created);

            ICacheProvider cacheProvider = CreateCacheProvider();

            FundingStreamPermissionUpdateModel updateModel = new FundingStreamPermissionUpdateModel()
            {
                CanApproveFunding = true,
                CanChooseFunding = true,
                CanCreateSpecification = false,
                CanEditCalculations = false,
                CanEditSpecification = false,
                CanMapDatasets = false,
                CanReleaseFunding = false,
                CanReleaseFundingForStatement = false,
                CanReleaseFundingForPaymentOrContract = false,
                CanApproveTemplates = true,
                CanCreateProfilePattern = false,
                CanEditProfilePattern = false,
                CanAssignProfilePattern = false,
                CanApplyCustomProfilePattern = false,
                CanApproveCalculations = false,
                CanApproveAnyCalculations = true,
                CanApproveAllCalculations = true
            };

            IVersionRepository<FundingStreamPermissionVersion> versionRepository = CreateFundingStreamPermissionRepository();

            FundingStreamPermissionService service = CreateService(
                userRepository,
                fundingStreamPermissionVersionRepository: versionRepository,
                cacheProvider: cacheProvider);

            // Act
            IActionResult result = await service.UpdatePermissionForUser(UserId, FundingStreamId, updateModel, null);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeEquivalentTo<FundingStreamPermissionCurrent>(new FundingStreamPermissionCurrent()
                {
                    UserId = UserId,
                    FundingStreamId = FundingStreamId,
                    CanApproveFunding = true,
                    CanChooseFunding = true,
                    CanCreateSpecification = false,
                    CanEditCalculations = false,
                    CanEditSpecification = false,
                    CanMapDatasets = false,
                    CanReleaseFunding = false,
                    CanReleaseFundingForStatement = false,
                    CanReleaseFundingForPaymentOrContract = false,
                    CanApproveTemplates = true,
                    CanCreateProfilePattern = false,
                    CanEditProfilePattern = false,
                    CanAssignProfilePattern = false,
                    CanApplyCustomProfilePattern = false,
                    CanApproveCalculations = false,
                    CanApproveAnyCalculations = true,
                    CanApproveAllCalculations = true
                });

            await userRepository
               .Received(0)
               .UpdateFundingStreamPermission(Arg.Any<FundingStreamPermission>());

            await cacheProvider
                .Received(0)
                .DeleteHashSet(Arg.Is($"{CacheKeys.EffectivePermissions}:{UserId}"));

            await versionRepository
               .Received(0)
               .SaveVersion(Arg.Any<FundingStreamPermissionVersion>(), Arg.Is(UserId));

            await versionRepository
               .Received(0)
               .GetNextVersionNumber(Arg.Any<FundingStreamPermissionVersion>(), 0, Arg.Is(UserId));
        }

        [TestMethod]
        public async Task UpdatePermissionForUser_WhenSavingPermissionsFails_ThenInternalServerErrorReturned()
        {
            // Arrange
            IUserRepository userRepository = CreateUserRepository();
            User user = new User()
            {
                UserId = UserId
            };

            userRepository
                .GetUserById(Arg.Is(UserId))
                .Returns(user);

            FundingStreamPermission existingPermission = new FundingStreamPermission()
            {
                UserId = UserId,
                FundingStreamId = FundingStreamId,
                CanApproveFunding = true,
                CanChooseFunding = false,
                CanCreateSpecification = false,
                CanEditCalculations = false,
                CanEditSpecification = false,
                CanMapDatasets = false,
                CanReleaseFunding = false,
                CanReleaseFundingForStatement = false,
                CanReleaseFundingForPaymentOrContract = false,
                CanCreateProfilePattern = false,
                CanEditProfilePattern = false,
                CanAssignProfilePattern = false,
                CanApplyCustomProfilePattern = false,
                CanApproveCalculations = true,
                CanApproveAnyCalculations = false,
                CanApproveAllCalculations = false
            };

            userRepository
                .GetFundingStreamPermission(Arg.Is(UserId), Arg.Is(FundingStreamId))
                .Returns(existingPermission);

            userRepository
                .UpdateFundingStreamPermission(Arg.Any<FundingStreamPermission>())
                .Returns(HttpStatusCode.InternalServerError);

            ICacheProvider cacheProvider = CreateCacheProvider();

            FundingStreamPermissionUpdateModel updateModel = new FundingStreamPermissionUpdateModel()
            {
                CanApproveFunding = true,
                CanChooseFunding = true,
                CanCreateSpecification = false,
                CanEditCalculations = false,
                CanEditSpecification = false,
                CanMapDatasets = false,
                CanReleaseFunding = false,
                CanReleaseFundingForStatement = false,
                CanReleaseFundingForPaymentOrContract = false,
                CanCreateProfilePattern = false,
                CanEditProfilePattern = false,
                CanAssignProfilePattern = false,
                CanApplyCustomProfilePattern = false,
                CanApproveCalculations = false,
                CanApproveAnyCalculations = true,
                CanApproveAllCalculations = true
            };

            FundingStreamPermissionService service = CreateService(userRepository, cacheProvider: cacheProvider);

            // Act
            IActionResult result = await service.UpdatePermissionForUser(UserId, FundingStreamId, updateModel, null);

            // Assert
            result
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be("Saving funding stream permission to repository returned 'InternalServerError'");

            await userRepository
               .Received(1)
               .UpdateFundingStreamPermission(Arg.Any<FundingStreamPermission>());

            await cacheProvider
                .Received(0)
                .DeleteHashSet(Arg.Is($"{CacheKeys.EffectivePermissions}:{UserId}"));
        }

        [TestMethod]
        public async Task UpdatePermissionForUser_WhenPermissionsAreSetOnAFundingStreamAndRemoveAllExistingPermissions_ThenPermissionsDeleteInRepository()
        {
            // Arrange
            IUserRepository userRepository = CreateUserRepository();
            User user = new User()
            {
                UserId = UserId
            };

            userRepository
                .GetUserById(Arg.Is(UserId))
                .Returns(user);

            FundingStreamPermission existingPermission = new FundingStreamPermission()
            {
                UserId = UserId,
                FundingStreamId = FundingStreamId,
                CanApproveFunding = true,
                CanChooseFunding = true,
                CanCreateSpecification = false,
                CanEditCalculations = false,
                CanEditSpecification = false,
                CanMapDatasets = false,
                CanReleaseFunding = false,
                CanReleaseFundingForStatement = false,
                CanReleaseFundingForPaymentOrContract = false,
                CanApproveTemplates = true,
                CanCreateProfilePattern = false,
                CanEditProfilePattern = false,
                CanAssignProfilePattern = false,
                CanApplyCustomProfilePattern = false,
                CanApproveCalculations = false,
                CanApproveAnyCalculations = true,
                CanApproveAllCalculations = true
            };

            userRepository
                .GetFundingStreamPermission(Arg.Is(UserId), Arg.Is(FundingStreamId), Arg.Is(true))
                .Returns(existingPermission);

            userRepository
                .DeleteFundingStreamPermission(Arg.Any<FundingStreamPermission>())
                .Returns(HttpStatusCode.OK);

            ICacheProvider cacheProvider = CreateCacheProvider();

            FundingStreamPermissionUpdateModel updateModel = new FundingStreamPermissionUpdateModel()
            {
                CanApproveFunding = false,
                CanChooseFunding = false,
                CanCreateSpecification = false,
                CanEditCalculations = false,
                CanEditSpecification = false,
                CanMapDatasets = false,
                CanReleaseFunding = false,
                CanReleaseFundingForStatement = false,
                CanReleaseFundingForPaymentOrContract = false,
                CanApproveTemplates = false,
                CanCreateProfilePattern = false,
                CanEditProfilePattern = false,
                CanAssignProfilePattern = false,
                CanApplyCustomProfilePattern = false,
                CanApproveCalculations = false,
                CanApproveAnyCalculations = false,
                CanApproveAllCalculations = false
            };

            IVersionRepository<FundingStreamPermissionVersion> versionRepository = CreateFundingStreamPermissionRepository();

            FundingStreamPermissionService service = CreateService(
                userRepository,
                fundingStreamPermissionVersionRepository: versionRepository,
                cacheProvider: cacheProvider);

            // Act
            IActionResult result = await service.UpdatePermissionForUser(UserId, FundingStreamId, updateModel, null);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeEquivalentTo<FundingStreamPermissionCurrent>(new FundingStreamPermissionCurrent()
                {
                    UserId = UserId,
                    FundingStreamId = FundingStreamId,
                    CanApproveFunding = false,
                    CanChooseFunding = false,
                    CanCreateSpecification = false,
                    CanEditCalculations = false,
                    CanEditSpecification = false,
                    CanMapDatasets = false,
                    CanReleaseFunding = false,
                    CanReleaseFundingForStatement = false,
                    CanReleaseFundingForPaymentOrContract = false,
                    CanApproveTemplates = false,
                    CanCreateProfilePattern = false,
                    CanEditProfilePattern = false,
                    CanAssignProfilePattern = false,
                    CanApplyCustomProfilePattern = false,
                    CanApproveCalculations = false,
                    CanApproveAnyCalculations = false,
                    CanApproveAllCalculations = false
                });

            await userRepository
               .Received(1)
               .DeleteFundingStreamPermission(Arg.Is<FundingStreamPermission>(x => x.UserId == UserId && x.FundingStreamId == FundingStreamId));

            await cacheProvider
                .Received(1)
                .DeleteHashSet(Arg.Is($"{CacheKeys.EffectivePermissions}:{UserId}"));

            await versionRepository
               .Received(1)
               .SaveVersion(Arg.Any<FundingStreamPermissionVersion>(), Arg.Is(UserId));

            await versionRepository
               .Received(1)
               .GetNextVersionNumber(Arg.Any<FundingStreamPermissionVersion>(), 0, Arg.Is(UserId));
        }
    }
}
