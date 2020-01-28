using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Models.Users;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Common.Caching;
using CalculateFunding.Services.Users.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
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
                    !p.CanReleaseFunding
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
                   !p.CanReleaseFunding
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
    }
}
