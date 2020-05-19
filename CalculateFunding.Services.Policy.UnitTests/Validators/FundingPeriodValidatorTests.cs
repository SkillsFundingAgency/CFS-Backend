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
    public class FundingPeriodValidatorTests
    {
        [TestMethod]
        public async Task ValidateAsync_WhenNameIsEmpty_ValidIsFalse()
        {
            //Arrange
            List<FundingPeriod> fundingPeriods = CreateModel();
            FundingPeriodsJsonModel model = new FundingPeriodsJsonModel { FundingPeriods = fundingPeriods.ToArray() };
            model.FundingPeriods[0].Name = string.Empty;

            IValidator<FundingPeriodsJsonModel> validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors.Select(x => x.PropertyName == "FundingPeriods[0].Name" && x.ErrorMessage == "No funding name was provided for the FundingPeriod")
                .Count()
                .Should()
                .Be(1);
        }

        [TestMethod]
        public async Task ValidateAsync_WhenModelFieldsIsEmpty_ValidIsFalse()
        {
            //Arrange
            List<FundingPeriod> fundingPeriods = CreateModel();
            FundingPeriodsJsonModel model = new FundingPeriodsJsonModel { FundingPeriods = fundingPeriods.ToArray() };
            
            model.FundingPeriods[0].Name = string.Empty;
            model.FundingPeriods[0].Period = string.Empty;           

            IValidator<FundingPeriodsJsonModel> validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

           

            result
                .Errors.Select(x => x.PropertyName == "FundingPeriods[0].Name" && x.ErrorMessage == "No funding name was provided for the FundingPeriod")
                .Contains(true).Should().Be(true);

            result
                .Errors.Select(x => x.PropertyName == "FundingPeriods[0].Period" && x.ErrorMessage == "No funding period was provided for the FundingPeriod")
                .Contains(true).Should().Be(true);

            
        }

        [TestMethod]
        public async Task ValidateAsync_WhenModelFieldsIsNull_ValidIsFalse()
        {
            //Arrange

            List<FundingPeriod> fundingPeriods = CreateModel();
            FundingPeriod nullStartDatefundingPeriod = new FundingPeriod()
            {
                Id = "AY2017181",
                Name = "Academic 2017/18",
                Type = FundingPeriodType.AY,
                Period = "1980",               
                EndDate = DateTimeOffset.Now.Date
            };
            FundingPeriod nullEndDatefundingPeriod = new FundingPeriod()
            {
                Id = "AY2017181",
                Name = "Academic 2017/18",
                Type = FundingPeriodType.AY,
                Period = "1980",
                StartDate = DateTimeOffset.Now.Date
            };

            fundingPeriods.Add(nullStartDatefundingPeriod);
            fundingPeriods.Add(nullEndDatefundingPeriod);

            FundingPeriodsJsonModel model = new FundingPeriodsJsonModel { FundingPeriods = fundingPeriods.ToArray() };
            
            model.FundingPeriods[0].Name = null;
            model.FundingPeriods[0].Period = null;
            model.FundingPeriods[1].Type = null;



            IValidator<FundingPeriodsJsonModel> validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors.Select(x => x.PropertyName == "FundingPeriods[0].Name" && x.ErrorMessage == "No funding name was provided for the FundingPeriod")
               .Contains(true).Should().Be(true);

            result
                .Errors.Select(x => x.PropertyName == "FundingPeriods[0].Period" && x.ErrorMessage == "No funding period was provided for the FundingPeriod")
                .Contains(true).Should().Be(true);

            result
               .Errors.Select(x => x.PropertyName == "FundingPeriods[1].Type" && x.ErrorMessage == "Null funding type was provided for the FundingPeriod")
               .Contains(true).Should().Be(true);

            result
               .Errors.Select(x => x.PropertyName == "FundingPeriods[2].StartDate" && x.ErrorMessage == "No funding start date was provided for the FundingPeriod")
               .Contains(true).Should().Be(true);

            result
              .Errors.Select(x => x.PropertyName == "FundingPeriods[3].EndDate" && x.ErrorMessage == "No funding end date was provided for the FundingPeriod")
              .Contains(true).Should().Be(true);
        }

        [TestMethod]       
        public async Task ValidateAsync_IsValidIsTrue()
        {
            //Arrange
            List<FundingPeriod> fundingPeriods = CreateModel();
            FundingPeriodsJsonModel model = new FundingPeriodsJsonModel { FundingPeriods = fundingPeriods.ToArray() };
            
            IValidator<FundingPeriodsJsonModel> validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();

        }

        private List<FundingPeriod> CreateModel()
        {
            List<FundingPeriod> periods = new List<FundingPeriod>
            {
                new FundingPeriod()
                {
                     Id = "AY2017181",
                     Name = "Academic 2017/18",
                     Type= FundingPeriodType.AY,
                     Period = "1980",
                     StartDate = DateTimeOffset.Now.Date,
                     EndDate = DateTimeOffset.Now.Date
                },
                new FundingPeriod()
                {
                     Id = "AY2018191",
                     Name = "Academic 2018/19",
                     Type= FundingPeriodType.AY,
                     Period = "1980",
                     StartDate = DateTimeOffset.Now.Date,
                     EndDate = DateTimeOffset.Now.Date
                }
               
            };      

            return periods;
        }

        private static FundingPeriodJsonModelValidator CreateValidator()
        {
            return new FundingPeriodJsonModelValidator();
        }
    }
}