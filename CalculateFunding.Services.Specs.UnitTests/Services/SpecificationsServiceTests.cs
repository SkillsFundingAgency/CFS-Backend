using AutoMapper;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Logging;
using CalculateFunding.Services.Specs.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Serilog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog.Debugging;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Specs.Services
{
    [TestClass]
    public class SpecificationsServiceTests
    {

        static SpecificationsService CreateService(IMapper mapper = null, ISpecificationsRepository specifcationsRepository = null, 
            ILogger logs = null, IValidator<PolicyCreateModel> policyCreateModelValidator = null,
            IValidator<SpecificationCreateModel> specificationCreateModelvalidator = null, IValidator<CalculationCreateModel> calculationCreateModelValidator = null)
        {
            return new SpecificationsService(mapper ?? CreateMapper(), specifcationsRepository ?? CreateSpecificationsRepository(), logs ?? CreateLogger(), policyCreateModelValidator ?? CreatePolicyValidator(),
                specificationCreateModelvalidator ?? CreateSpecificationValidator(), calculationCreateModelValidator ?? CreateCalculationValidator());
        }

        static IMapper CreateMapper()
        {
            return Substitute.For<IMapper>();
        }

        static ISpecificationsRepository CreateSpecificationsRepository()
        {
            return Substitute.For<ISpecificationsRepository>();
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

       
        static IValidator<PolicyCreateModel> CreatePolicyValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
                validationResult = new ValidationResult();

            IValidator<PolicyCreateModel> validator = Substitute.For<IValidator<PolicyCreateModel>>();

            validator
               .ValidateAsync(Arg.Any<PolicyCreateModel>())
               .Returns(validationResult);

            return validator;
        }

        static IValidator<SpecificationCreateModel> CreateSpecificationValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
                validationResult = new ValidationResult();

            IValidator<SpecificationCreateModel> validator = Substitute.For<IValidator<SpecificationCreateModel>>();

            validator
               .ValidateAsync(Arg.Any<SpecificationCreateModel>())
               .Returns(validationResult);

            return validator;
        }

        static IValidator<CalculationCreateModel> CreateCalculationValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
                validationResult = new ValidationResult();

            IValidator<CalculationCreateModel> validator = Substitute.For<IValidator<CalculationCreateModel>>();

            validator
               .ValidateAsync(Arg.Any<CalculationCreateModel>())
               .Returns(validationResult);

            return validator;
        }
    }

   
}
