using CalculateFunding.Models.Users;
using CalculateFunding.Services.Users.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Users
{
    public partial class FundingStreamPermissionServiceTests
    {
        [TestMethod]
        public async Task DownloadEffectivePermissionsForFundingStream_WhenFundingStreamIdIsIsEmpty_ThenBadRequestReturned()
        {
            // Arrange
            FundingStreamPermissionService service = CreateService();


            // Act
            IActionResult result = await service.DownloadEffectivePermissionsForFundingStream(null);

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
        public async Task DownloadEffectivePermissionsForFundingStream_WhenEffectivePermissionFound_ThenOkResultReturned()
        {
            // Arrange
            string fileName = new RandomString();

            IUsersCsvGenerator usersCsvGenerator = CreateUsersCsvGenerator();
            FundingStreamPermissionCurrentDownloadModel downloadModel = new FundingStreamPermissionCurrentDownloadModel()
            {
                FileName = fileName
            };

            usersCsvGenerator
                .Generate(Arg.Is<UserPermissionCsvGenerationMessage>(_ => _.FundingStreamId == FundingStreamId))
                .Returns(downloadModel);

            FundingStreamPermissionService service = CreateService(usersCsvGenerator: usersCsvGenerator);

            // Act
            IActionResult result = await service.DownloadEffectivePermissionsForFundingStream(FundingStreamId);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeEquivalentTo(downloadModel);
        }
    }
}
