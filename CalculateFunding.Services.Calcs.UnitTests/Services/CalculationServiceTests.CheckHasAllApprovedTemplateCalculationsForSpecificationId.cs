using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models;
using CalculateFunding.Models.Messages;
using CalculateFunding.Services.Calcs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

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

        [TestMethod]
        [DataRow("SpecId1", DeletionType.SoftDelete)]
        [DataRow("SpecId1", DeletionType.PermanentDelete)]
        public async Task DeleteCalculations_Deletes_Dependencies_Using_Correct_SpecificationId_And_DeletionType(string specificationId, DeletionType deletionType)
        {
            Message message = new Message
            {
                UserProperties =
                {
                    new KeyValuePair<string, object>("jobId", JobId),
                    new KeyValuePair<string, object>("specification-id", specificationId),
                    new KeyValuePair<string, object>("deletion-type", (int)deletionType)
                }
            };
            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            CalculationService calculationService = CreateCalculationService(calculationsRepository: calculationsRepository);

            await calculationService.DeleteCalculations(message);

            await calculationsRepository.Received(1).DeleteCalculationsBySpecificationId(specificationId, deletionType);
        }
    }
}
