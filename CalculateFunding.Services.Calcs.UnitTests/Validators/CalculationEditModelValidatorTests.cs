﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;

using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Calculation = CalculateFunding.Models.Calcs.Calculation;

namespace CalculateFunding.Services.Calcs.Validators
{
    [TestClass]
    public class CalculationEditModelValidatorTests
    {
        [TestMethod]
        public async Task ValidateAsync_WhenNameIsEmpty_ValidIsFalse()
        {
            //Arrange
            CalculationEditModel model = CreateModel();
            model.Name = string.Empty;

            CalculationEditModelValidator validator = CreateValidator();

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
            CalculationEditModel model = CreateModel();
            model.SpecificationId = string.Empty;

            CalculationEditModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public async Task ValidateAsync_WhenCalculationIdEmpty_ValidIsFalse()
        {
            //Arrange
            CalculationEditModel model = CreateModel();
            model.CalculationId = string.Empty;

            CalculationEditModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public async Task ValidateAsync_WhenValueTypeIsMissing_ValidIsFalse()
        {
            //Arrange
            CalculationEditModel model = CreateModel();
            model.ValueType = null;

            CalculationEditModelValidator validator = CreateValidator();

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
            CalculationEditModel model = CreateModel();
            model.SourceCode = string.Empty;

            CalculationEditModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();
        }

        [DataTestMethod]
        [DataRow("\"")]
        public async Task ValidateAsync_WhenCalculationNameContainsNotAllowedCharacters_ValidIsFalse(string calculationNameNotAllowedCharacter)
        {
            //Arrange
            CalculationEditModel model = CreateModel();
            model.Name += calculationNameNotAllowedCharacter;

            CalculationEditModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result.Errors
              .Should()
              .Contain(_ => _.ErrorMessage == $"Calculation name contains not allowed character: '{calculationNameNotAllowedCharacter}'");
        }

        [TestMethod]
        public async Task ValidateAsync_WhenCalculationNameLengthIsLessThanAllowedLimit_ValidIsFalse()
        {
            //Arrange
            CalculationEditModel model = CreateModel();
            model.Name = "123";

            Calculation existingCalculation = new Calculation
            {
                Id = model.CalculationId,
                Current = new CalculationVersion
                {
                    CalculationType = CalculationType.Additional
                }
            };

            ICalculationsRepository calculationsRepository = CreateCalculationRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(model.CalculationId))
                .Returns(existingCalculation);

            CalculationEditModelValidator validator = CreateValidator(calculationRepository: calculationsRepository);

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result.Errors
              .Should()
              .Contain(_ => _.ErrorMessage == "Calculation name length should be at least 4 characters");
        }

        [TestMethod]
        public async Task ValidateAsync_WhenCalculationNameAlreadyExists_ValidIsFalse()
        {
            //Arrange
            CalculationEditModel model = CreateModel();

            Calculation calculationWithSameName = new Calculation();

            ICalculationsRepository calculationsRepository = CreateCalculationRepository();
            calculationsRepository
                .GetCalculationBySpecificationIdAndCalculationName(Arg.Is(model.SpecificationId), Arg.Is(model.Name))
                .Returns(calculationWithSameName);

            CalculationEditModelValidator validator = CreateValidator(calculationRepository: calculationsRepository);

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public async Task ValidateAsync_WhenCalculationSourceCodeNameAlreadyExists_ValidIsFalse()
        {
            //Arrange
            CalculationEditModel model = CreateModel();
            string sourceCodeName = new VisualBasicTypeIdentifierGenerator().GenerateIdentifier(model.Name);

            Calculation calculationWithSameName = new Calculation();

            ICalculationsRepository calculationsRepository = CreateCalculationRepository();
            calculationsRepository
                .GetCalculationBySpecificationIdAndCalculationSourceCodeName(Arg.Is(model.SpecificationId), Arg.Is(sourceCodeName))
                .Returns(calculationWithSameName);

            CalculationEditModelValidator validator = CreateValidator(calculationRepository: calculationsRepository);

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
            CalculationEditModel model = CreateModel();

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
           
            CalculationEditModelValidator validator = CreateValidator(previewService: previewService);

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
            CalculationEditModel model = CreateModel();

            ICalculationsRepository calculationsRepository = CreateCalculationRepository();
            calculationsRepository
                .GetCalculationBySpecificationIdAndCalculationName(Arg.Is(model.SpecificationId), Arg.Is(model.Name))
                .Returns((Calculation)null);

            CalculationEditModelValidator validator = CreateValidator(calculationsRepository);

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public async Task ValidateAsync_WhenCalculationNameAlreadyExistsButItsTheSameCalc_EnsuresValidIsTrue()
        {
            //Arrange
            CalculationEditModel model = CreateModel();

            Calculation calculationWithSameName = new Calculation
            {
                Id = model.CalculationId
            };

            ICalculationsRepository calculationsRepository = CreateCalculationRepository();
            calculationsRepository
                .GetCalculationBySpecificationIdAndCalculationName(Arg.Is(model.SpecificationId), Arg.Is(model.Name))
                .Returns(calculationWithSameName);

            CalculationEditModelValidator validator = CreateValidator(calculationRepository: calculationsRepository);

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }

        private static CalculationEditModelValidator CreateValidator(
            ICalculationsRepository calculationRepository = null,
            IPreviewService previewService = null)
        {
            return new CalculationEditModelValidator(
                previewService ?? CreatePreviewService(),
                calculationRepository ?? CreateCalculationRepository());
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

        private static CalculationEditModel CreateModel()
        {
            return new CalculationEditModel
            {
                Description = "test description",
                CalculationId = "cal-1",
                Name = "test calc",
                SourceCode = "return 1000",
                SpecificationId = "spec-1",
                ValueType = CalculationValueType.Currency
            };
        }
    }
}
