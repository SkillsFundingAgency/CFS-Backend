using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [TestMethod]
        public async Task CreateAdditionalCalculation_GivenValidationFails_ReturnsBadRequest()
        {
            //Arrange
            CalculationCreateModel model = new CalculationCreateModel();
            Reference author = new Reference();

            ValidationResult validationResult = new ValidationResult(new[]{
                    new ValidationFailure("prop1", "oh no an error!!!")
                });

            IValidator<CalculationCreateModel> validator = CreateCalculationCreateModelValidator(validationResult);

            CalculationService calculationService = CreateCalculationService(calculationCreateModelValidator: validator);

            //Act
            IActionResult result = await calculationService.CreateAdditionalCalculation(model, author);

            //Assert
            result
                .Should()
                .BeAssignableTo<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task CreateAdditionalCalculation_GivenSavingDraftCalcFails_ReturnsInternalServerErrorResult()
        {
            //Arrange
            CalculationCreateModel model = CreateCalculationCreateModel();

            Reference author = CreateAuthor();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .CreateDraftCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.BadRequest);

            ILogger logger = CreateLogger();

            CalculationService calculationService = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository);

            string errorMessage = $"There was problem creating a new calculation with name {CalculationName} in Cosmos Db with status code 400";

            //Act
            IActionResult result = await calculationService.CreateAdditionalCalculation(model, author);

            //Assert
            result
                .Should()
                .BeAssignableTo<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be(errorMessage);

            logger
                .Received(1)
                .Error(Arg.Is(errorMessage));
        }

        [TestMethod]
        public async Task CreateAdditionalCalculation_GivenCalcSaves_ReturnsOKObjectResult()
        {
            //Arrange
            CalculationCreateModel model = CreateCalculationCreateModel();

            Reference author = CreateAuthor();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .CreateDraftCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            CalculationService calculationService = CreateCalculationService(
                calculationsRepository: calculationsRepository,
                calculationVersionRepository: versionRepository,
                searchRepository: searchRepository);

            //Act
            IActionResult result = await calculationService.CreateAdditionalCalculation(model, author);

            //Assert
            result
                .Should()
                .BeAssignableTo<OkObjectResult>();

            await
                versionRepository
                    .Received(1)
                    .SaveVersion(Arg.Is<CalculationVersion>(m =>
                        m.PublishStatus == PublishStatus.Draft &&
                        m.Author.Id == UserId &&
                        m.Author.Name == Username &&
                        m.Date.Date == DateTimeOffset.Now.Date &&
                        m.Version == 1 &&
                        m.SourceCode == model.SourceCode &&
                        m.Description == model.Description &&
                        m.ValueType == model.ValueType &&
                        m.CalculationType == CalculationType.Additional &&
                        m.WasTemplateCalculation == false &&
                        m.Namespace == CalculationNamespace.Additional &&
                        m.Name == model.Name &&
                        m.SourceCodeName == VisualBasicTypeGenerator.GenerateIdentifier(model.Name)
                    ));

            await
               searchRepository
                   .Received(1)
                   .Index(Arg.Is<IEnumerable<CalculationIndex>>(m =>
                       !string.IsNullOrWhiteSpace(m.First().Id) &&
                       m.First().Name == model.Name &&
                       m.First().SpecificationId == SpecificationId &&
                       m.First().ValueType == model.ValueType.ToString() &&
                       m.First().CalculationType == CalculationType.Additional.ToString() &&
                       m.First().Namespace == CalculationNamespace.Additional.ToString() &&
                       m.First().FundingStreamId == model.FundingStreamId &&
                       m.First().WasTemplateCalculation == false &&
                       m.First().Description == model.Description
                   ));

        }
    }
}
