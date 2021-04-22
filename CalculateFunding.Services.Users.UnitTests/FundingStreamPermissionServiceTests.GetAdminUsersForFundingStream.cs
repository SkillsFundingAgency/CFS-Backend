using CalculateFunding.Models.Users;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Tests.Common.Helpers;
using CalculateFunding.Services.Users.Interfaces;
using NSubstitute;
using System.Linq;

namespace CalculateFunding.Services.Users
{
    public partial class FundingStreamPermissionServiceTests
    {
        [TestMethod]
        public async Task GetAdminUsersForFundingStream_WhenFundingStreamIdIsEmpty_ThenBadRequestReturned()
        {
            // Arrange
            FundingStreamPermissionService service = CreateService();

            // Act
            IActionResult result = await service.GetAdminUsersForFundingStream(null);

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
        public async Task GetAdminUsersForFundingStream_WhenFundingStreamUserExists_ThenFundingStreamAdminsReturned()
        {
            // Arrange
            string userId = NewRandomString();
            string userName = NewRandomString();

            IUserRepository userRepository = CreateUserRepository();

            List<FundingStreamPermission> fundingStreamPermissions = new List<FundingStreamPermission>();
            fundingStreamPermissions.Add(new FundingStreamPermission()
            {
                UserId = userId,
                FundingStreamId = FundingStreamId,
                CanAdministerFundingStream = true
            });

            User user = new User
            {
                UserId = userId,
                Name = userName
            };

            userRepository
                .GetAdminFundingStreamPermissionsWithFundingStream(Arg.Is(FundingStreamId))
                .Returns(fundingStreamPermissions);

            userRepository
                .GetUserById(Arg.Is(userId))
                .Returns(user);

            FundingStreamPermissionService service = CreateService(
                userRepository: userRepository);

            // Act
            IActionResult result = await service.GetAdminUsersForFundingStream(FundingStreamId);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<List<User>>();

            List<User> actualUsers = (result as OkObjectResult).Value as List<User>;

            actualUsers
                .Count
                .Should()
                .Be(1);

            actualUsers
                .SingleOrDefault()
                .Should()
                .Be(user);
        }

        private static RandomString NewRandomString() => new RandomString();

    }
}
