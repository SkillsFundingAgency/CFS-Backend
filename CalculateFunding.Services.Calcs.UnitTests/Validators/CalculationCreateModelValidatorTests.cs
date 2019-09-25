using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Calcs.Interfaces;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Calcs.Validators
{
    [TestClass]
    public class CalculationCreateModelValidatorTests
    {
        [TestMethod]
        public async Task ValidateAsync_WhenNameIsEmpty_ValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            model.Name = string.Empty;

            CalculationCreateModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public async Task ValidateAsync_WhenSpecificationIsEmpty_ValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            model.SpecificationId = string.Empty;

            CalculationCreateModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public async Task ValidateAsync_WhenFundingStreamdIdEmpty_ValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            model.FundingStreamId = string.Empty;

            CalculationCreateModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();
        }
        
        [TestMethod]
        public async Task ValidateAsync_WhenFundingStreamIdEmptyForAdditionalCalcs_ValidIsTrue()
        {
            //Arrange
            CalculationCreateModel model = CreateModel(CalculationType.Additional);
            model.FundingStreamId = string.Empty;

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(model.SpecificationId))
                .Returns(new SpecificationSummary());

            CalculationCreateModelValidator validator = CreateValidator(specificationRepository: specificationRepository);

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public async Task ValidateAsync_WhenValueTypeIsMissing_ValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            model.ValueType = null;

            CalculationCreateModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public async Task ValidateAsync_WhenSourceCodeIsEmpty_ValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            model.SourceCode = string.Empty;

            CalculationCreateModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public async Task ValidateAsync_WhenCalculationNameAlreadyExists_ValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();

            Calculation calculationWithSameName = new Calculation();

            ICalculationsRepository calculationsRepository = CreateCalculationRepository();
            calculationsRepository
                .GetCalculationsBySpecificationIdAndCalculationName(Arg.Is(model.SpecificationId), Arg.Is(model.Name))
                .Returns(calculationWithSameName);

            CalculationCreateModelValidator validator = CreateValidator(calculationRepository: calculationsRepository);

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public async Task ValidateAsync_WhenSourceCodeDoesNotCompile_ValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();

            PreviewResponse previewResponse = new PreviewResponse
            {
                CompilerOutput = new Build
                {
                    CompilerMessages = new List<CompilerMessage>
                    {
                        new CompilerMessage { Message = "Failed" }
                    }
                }
            };

            IPreviewService previewService = CreatePreviewService(previewResponse);
           
            CalculationCreateModelValidator validator = CreateValidator(previewService: previewService);

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public async Task ValidateAsync_WhenSpecificationCanNotBeFound_ValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(model.SpecificationId))
                .Returns((SpecificationSummary)null);

            CalculationCreateModelValidator validator = CreateValidator(specificationRepository: specificationRepository);

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public async Task ValidateAsync_WhenSpecificationDoesNotContainFundingStreamValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();

            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                FundingStreams = new[] { new Reference() }
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(model.SpecificationId))
                .Returns(specificationSummary);

            CalculationCreateModelValidator validator = CreateValidator(specificationRepository: specificationRepository);

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public async Task ValidateAsync_WhenValidModel_ValidIsTrue()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();

           
            ICalculationsRepository calculationsRepository = CreateCalculationRepository();
            calculationsRepository
                .GetCalculationsBySpecificationIdAndCalculationName(Arg.Is(model.SpecificationId), Arg.Is(model.Name))
                .Returns((Calculation)null);

            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                Name = "spec name",
                FundingStreams = new[] { new Reference(model.FundingStreamId, "funding stream name") }
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(model.SpecificationId))
                .Returns(specificationSummary);

            CalculationCreateModelValidator validator = CreateValidator(
                calculationsRepository, specificationRepository: specificationRepository);

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();

            model.SpecificationName
                .Should()
                .Be("spec name");

            model.FundingStreamName
                .Should()
                .Be("funding stream name");
        }

        private static CalculationCreateModelValidator CreateValidator(
            ICalculationsRepository calculationRepository = null,
            IPreviewService previewService = null,
            ISpecificationRepository specificationRepository = null)
        {
            return new CalculationCreateModelValidator(
                calculationRepository ?? CreateCalculationRepository(),
                previewService ?? CreatePreviewService(),
                specificationRepository ?? CreateSpecificationRepository());
        }

        private static ICalculationsRepository CreateCalculationRepository()
        {
            return Substitute.For<ICalculationsRepository>();
        }

        private static IPreviewService CreatePreviewService(PreviewResponse previewResponse = null)
        {
            if (previewResponse == null)
            {
                previewResponse = new PreviewResponse
                {
                    CompilerOutput = new Build
                    {
                        CompilerMessages = new List<CompilerMessage>()
                    }
                };
            }

            OkObjectResult okObjectResult = new OkObjectResult(previewResponse);

            IPreviewService previewService = Substitute.For<IPreviewService>();
            previewService
                .Compile(Arg.Any<PreviewRequest>())
                .Returns(okObjectResult);

            return previewService;
        }

        private static ISpecificationRepository CreateSpecificationRepository()
        {
            return Substitute.For<ISpecificationRepository>();
        }

        private static CalculationCreateModel CreateModel(CalculationType calculationType = CalculationType.Template)
        {
            return new CalculationCreateModel
            {
                Description = "test description",
                FundingStreamId = "fs-1",
                Name = "test calc",
                SourceCode = "return 1000",
                SpecificationId = "spec-1",
                ValueType = CalculationValueType.Currency,
                CalculationType = calculationType
            };
        }
    }
}
