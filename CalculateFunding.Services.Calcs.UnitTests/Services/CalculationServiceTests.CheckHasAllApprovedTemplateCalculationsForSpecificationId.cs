using CalculateFunding.Models.Versioning;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Common.Caching;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [DataRow(5, false)]
        [DataRow(0, true)]
        [TestMethod]
        public async Task CheckHasAllApprovedTemplateCalculationsForSpecificationId_GivenResult_ReturnsExpectedResponseValue(int result, bool expected)
        {
            //Arrange
            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCountOfNonApprovedTemplateCalculations(Arg.Is(SpecificationId))
                .Returns(result);

            CalculationService calculationService = CreateCalculationService(calculationsRepository: calculationsRepository);

            //Act
            IActionResult actionResult = await calculationService.CheckHasAllApprovedTemplateCalculationsForSpecificationId(SpecificationId);

            //Assert
            BooleanResponseModel responseModel = (actionResult as OkObjectResult).Value as BooleanResponseModel;

            responseModel
                .Value
                .Should()
                .Be(expected);
        }
    }
}
