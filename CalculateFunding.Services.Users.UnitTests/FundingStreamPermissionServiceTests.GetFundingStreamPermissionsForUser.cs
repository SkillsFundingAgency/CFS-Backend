using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Users;
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
        public async Task GetFundingStreamPermissionsForUser_WhenUserIdIsNull_ThenBadRequestReturned()
        {
            // Arrange
            string userId = null;

            FundingStreamPermissionService service = CreateService();

            // Act
            IActionResult result = await service.GetFundingStreamPermissionsForUser(userId, null);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("userId is null or empty");
        }

        [TestMethod]
        public async Task GetFundingStreamPermissionsForUser_WhenNoResultsFound_ThenEmptyListReturned()
        {
            // Arrange
            List<FundingStreamPermission> repositoryResult = new List<FundingStreamPermission>();

            IUserRepository userRepository = CreateUserRepository();

            userRepository
                .GetFundingStreamPermissions(Arg.Is(UserId))
                .Returns(repositoryResult);

            FundingStreamPermissionService service = CreateService(userRepository);

            // Act
            IActionResult result = await service.GetFundingStreamPermissionsForUser(UserId, null);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeEquivalentTo<IEnumerable<FundingStreamPermissionCurrent>>(Enumerable.Empty<FundingStreamPermissionCurrent>());

            await userRepository
                .Received(1)
                .GetFundingStreamPermissions(Arg.Is(UserId));
        }

        [TestMethod]
        public async Task GetFundingStreamPermissionsForUser_WhenResultsFound_ThenFundingStreamPermissionsReturned()
        {
            // Arrange
            List<FundingStreamPermission> repositoryResult = new List<FundingStreamPermission>()
            {
                new FundingStreamPermission()
                {
                    CanApproveFunding = true,
                    CanChooseFunding = true,
                    CanCreateSpecification = false,
                    CanEditCalculations = false,
                    CanEditSpecification = false,
                    CanMapDatasets = false,
                    CanReleaseFunding = true,
                    FundingStreamId = FundingStreamId,
                    UserId = UserId
                },
                new FundingStreamPermission()
                {
                    CanApproveFunding = true,
                    CanChooseFunding = true,
                    CanCreateSpecification = true,
                    CanEditCalculations = true,
                    CanEditSpecification = false,
                    CanMapDatasets = false,
                    CanReleaseFunding = true,
                    FundingStreamId = "fs2",
                    UserId = UserId
                }
            };

            IUserRepository userRepository = CreateUserRepository();

            userRepository
                .GetFundingStreamPermissions(Arg.Is(UserId))
                .Returns(repositoryResult);

            FundingStreamPermissionService service = CreateService(userRepository);

            // Act
            IActionResult result = await service.GetFundingStreamPermissionsForUser(UserId, null);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeEquivalentTo<IEnumerable<FundingStreamPermissionCurrent>>(new List<FundingStreamPermissionCurrent>()
                {
                    new FundingStreamPermissionCurrent()
                    {
                        CanApproveFunding = true,
                        CanChooseFunding = true,
                        CanCreateSpecification = false,
                        CanEditCalculations = false,
                        CanEditSpecification = false,
                        CanMapDatasets = false,
                        CanReleaseFunding = true,
                        FundingStreamId = FundingStreamId,
                        UserId = UserId
                    },
                    new FundingStreamPermissionCurrent()
                    {
                        CanApproveFunding = true,
                        CanChooseFunding = true,
                        CanCreateSpecification = true,
                        CanEditCalculations = true,
                        CanEditSpecification = false,
                        CanMapDatasets = false,
                        CanReleaseFunding = true,
                        FundingStreamId = "fs2",
                        UserId = UserId
                    }
                });

            await userRepository
                .Received(1)
                .GetFundingStreamPermissions(Arg.Is(UserId));
        }
    }
}
