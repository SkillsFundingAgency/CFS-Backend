using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [DataTestMethod]
        [DataRow("block 1%", "Block1Percent")]
        [DataRow(null, null)]
        public async Task GenerateCalculationIdentifier_JobActionQueuesJob_ReturnsOKJobDetails(
            string calculationName,
            string expectedSourceCodeName)
        {
            GenerateIdentifierModel generateIdentifierModel = new GenerateIdentifierModel {CalculationName = calculationName };

            CalculationService calculationService = CreateCalculationService();

            IActionResult actionResult = calculationService.GenerateCalculationIdentifier(generateIdentifierModel);

            actionResult
                .Should()
                .BeAssignableTo<OkObjectResult>()
                .And
                .NotBeNull();

            OkObjectResult okObjectResult = actionResult as OkObjectResult;

            okObjectResult.Value.Should().NotBeNull().And.BeAssignableTo<CalculationIdentifier>();

            CalculationIdentifier actualJob = okObjectResult.Value as CalculationIdentifier;

            actualJob.Name.Should().Be(calculationName);
            actualJob.SourceCodeName.Should().Be(expectedSourceCodeName);

        }

    }
}
