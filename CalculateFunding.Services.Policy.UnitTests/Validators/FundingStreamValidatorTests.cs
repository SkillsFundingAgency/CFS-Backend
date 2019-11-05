using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Policy;
using CalculateFunding.Services.Providers.Validators;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Policy.Validators
{
    [TestClass]
    public class FundingStreamValidatorTests
    {
        private FundingStream _fundingStream;
        private FundingStreamSaveModelValidator _validator;

        private ValidationResult _validationResult;

        [TestInitialize]
        public void SetUp()
        {
            _validator = new FundingStreamSaveModelValidator();
        }

        [TestMethod]
        public async Task ValidateAsync_WhenNameIsEmpty_ValidIsFalse()
        {
            //Arrange
            FundingStreamSaveModel model = CreateModel();
            model.Name = string.Empty;

            IValidator<FundingStreamSaveModel> validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors.Select(x => x.PropertyName == "Name" && x.ErrorMessage == "No name was provided for the FundingStream")
                .Count()
                .Should()
                .Be(1);
        }

        [TestMethod]
        public async Task ValidateAsync_WhenModelFieldsIsEmpty_ValidIsFalse()
        {
            //Arrange
            FundingStreamSaveModel model = CreateModel();
            model.Id = string.Empty;
            model.Name = string.Empty;
            model.ShortName = string.Empty;

            IValidator<FundingStreamSaveModel> validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();



            result
                .Errors.Select(x => x.PropertyName == "Name" && x.ErrorMessage == "No name was provided for the FundingStream")
                .Contains(true).Should().Be(true);

            result
                .Errors.Select(x => x.PropertyName == "ShortName" && x.ErrorMessage == "No short name was provided for the FundingStream")
                .Contains(true).Should().Be(true);

            result
                .Errors.Select(x => x.PropertyName == "Id" && x.ErrorMessage == "No id was provided for the FundingStream")
                .Contains(true).Should().Be(true);

        }

        [TestMethod]
        public async Task ValidateAsync_WhenModelFieldsIsNull_ValidIsFalse()
        {
            //Arrange
            FundingStreamSaveModel model = CreateModel();
            model.Id = null;
            model.Name = null;
            model.ShortName = null;

            IValidator<FundingStreamSaveModel> validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();



            result
                .Errors.Select(x => x.PropertyName == "Name" && x.ErrorMessage == "No name was provided for the FundingStream")
                .Contains(true).Should().Be(true);

            result
                .Errors.Select(x => x.PropertyName == "ShortName" && x.ErrorMessage == "No short name was provided for the FundingStream")
                .Contains(true).Should().Be(true);

            result
                .Errors.Select(x => x.PropertyName == "Id" && x.ErrorMessage == "No id was provided for the FundingStream")
                .Contains(true).Should().Be(true);

        }

        [TestMethod]
        public async Task ValidateAsync_IsValidIsTrue()
        {
            //Arrange
            FundingStreamSaveModel model = CreateModel();         

            IValidator<FundingStreamSaveModel> validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();

        }

        private FundingStreamSaveModel CreateModel()
        {
            FundingStreamSaveModel fundingStream = new FundingStreamSaveModel()
            {                
                Id = "AY2017181",
                Name = "Academic 2017/18",
                ShortName = "NC"
            };

            return fundingStream;
        }

        private IValidator<FundingStreamSaveModel> CreateFundingStreamSaveModelValidator()
        {
            ValidationResult validationResult = null;
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<FundingStreamSaveModel> validator = Substitute.For<IValidator<FundingStreamSaveModel>>();

            validator
               .ValidateAsync(Arg.Any<FundingStreamSaveModel>())
               .Returns(validationResult);

            return validator;
        }

        private static FundingStreamSaveModelValidator CreateValidator()
        {
            return new FundingStreamSaveModelValidator();
        }
    }
}