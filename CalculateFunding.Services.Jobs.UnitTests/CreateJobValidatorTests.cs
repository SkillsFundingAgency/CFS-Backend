using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Jobs.Validators;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Services.Jobs
{
    [TestClass]
    public class CreateJobValidatorTests
    {
        [TestMethod]
        public void CreateJobValidator_GivenNullJobCreateModel_ValidIsFalse()
        {
            //Arrange
            CreateJobValidationModel createJobValidationModel = CreateNewCreateJobValidationModel();
            createJobValidationModel.JobCreateModel = null;

            CreateJobValidator validator = new CreateJobValidator();

            //Act
            ValidationResult result = validator.Validate(createJobValidationModel);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors
                .Count
                .Should()
                .Be(1);

            result
                .Errors
                .First()
                .ErrorMessage
                .Should()
                .Be("Null job create model was provided");
        }

        [TestMethod]
        public void CreateJobValidator_GivenNullJobDefinition_ValidIsFalse()
        {
            //Arrange
            CreateJobValidationModel createJobValidationModel = CreateNewCreateJobValidationModel();
            createJobValidationModel.JobDefinition = null;

            CreateJobValidator validator = new CreateJobValidator();

            //Act
            ValidationResult result = validator.Validate(createJobValidationModel);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
               .Errors
               .Count
               .Should()
               .Be(1);

            result
                .Errors
                .First()
                .ErrorMessage
                .Should()
                .Be("Null job definition was provided");
        }

        [TestMethod]
        public void CreateJobValidator_GivenDefinitionRequiresMessageBodyButNoMessageBodyProvided_ValidIsFalse()
        {
            //Arrange
            CreateJobValidationModel createJobValidationModel = CreateNewCreateJobValidationModel();
            createJobValidationModel.JobCreateModel.MessageBody = "";

            CreateJobValidator validator = new CreateJobValidator();

            //Act
            ValidationResult result = validator.Validate(createJobValidationModel);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors
                .Count
                .Should()
                .Be(1);

            result
               .Errors
               .First()
               .ErrorMessage
               .Should()
               .Be("A message body is required when using job definition: 'job-def-1'");
        }

        [TestMethod]
        public void CreateJobValidator_GivenDefinitionRequiresSpecificationIdButNoSpecificationIdWasProvided_ValidIsFalse()
        {
            //Arrange
            CreateJobValidationModel createJobValidationModel = CreateNewCreateJobValidationModel();
            createJobValidationModel.JobCreateModel.SpecificationId = "";

            CreateJobValidator validator = new CreateJobValidator();

            //Act
            ValidationResult result = validator.Validate(createJobValidationModel);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors
                .Count
                .Should()
                .Be(1);

            result
                .Errors
                .First()
                .ErrorMessage
                .Should()
                .Be("A specification id is required when using job definition: 'job-def-1'");
        }

        [TestMethod]
        public void CreateJobValidator_GivenDefinitionRequiresEntityIdButNoEntityIdWasProvided_ValidIsFalse()
        {
            //Arrange
            CreateJobValidationModel createJobValidationModel = CreateNewCreateJobValidationModel();
            createJobValidationModel.JobCreateModel.Trigger.EntityId = "";

            CreateJobValidator validator = new CreateJobValidator();

            //Act
            ValidationResult result = validator.Validate(createJobValidationModel);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors
                .Count
                .Should()
                .Be(1);

            result
                .Errors
                .First()
                .ErrorMessage
                .Should()
                .Be("An entity id is required when using job definition: 'job-def-1'");
        }

        [TestMethod]
        public void CreateJobValidator_GivenDefinitionRequiresMessagePropertiesButNoneWereSet_ValidIsFalse()
        {
            //Arrange
            CreateJobValidationModel createJobValidationModel = CreateNewCreateJobValidationModel();
            createJobValidationModel.JobCreateModel.Properties = null;

            CreateJobValidator validator = new CreateJobValidator();

            //Act
            ValidationResult result = validator.Validate(createJobValidationModel);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors
                .Count
                .Should()
                .Be(1);

            result
                .Errors
                .First()
                .ErrorMessage
                .Should()
                .Be("Message properties are required when using job definition: 'job-def-1'");
        }

        [TestMethod]
        public void CreateJobValidator_GivenDefinitionRequiresMessagePropertiesButPropertiesSupplieddoNotMatch_ValidIsFalse()
        {
            //Arrange
            CreateJobValidationModel createJobValidationModel = CreateNewCreateJobValidationModel();
            createJobValidationModel.JobCreateModel.Properties = new Dictionary<string, string>
            {
                {"prop-1", "property 1" }
            };

            CreateJobValidator validator = new CreateJobValidator();

            //Act
            ValidationResult result = validator.Validate(createJobValidationModel);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors
                .Count
                .Should()
                .Be(1);

            result
                .Errors
                .First()
                .ErrorMessage
                .Should()
                .Be("Message property 'prop-2' is required when using job definition: 'job-def-1'");
        }

        [TestMethod]
        public void CreateJobValidator_GivenDefinitionButMissingSessionProperty_ValidIsFalse()
        {
            //Arrange
            CreateJobValidationModel createJobValidationModel = CreateNewCreateJobValidationModel();
            createJobValidationModel.JobDefinition.SessionMessageProperty = "prop-3";

            CreateJobValidator validator = new CreateJobValidator();

            //Act
            ValidationResult result = validator.Validate(createJobValidationModel);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors
                .Count
                .Should()
                .Be(1);

            result
                .Errors
                .First()
                .ErrorMessage
                .Should()
                .Be($"Session Message property 'prop-3' is required when using job definition: 'job-def-1'");
        }

        [TestMethod]
        public void CreateJobValidator_GivenValidModelWithSessionConfigured_ValidIsTrue()
        {
            //Arrange
            CreateJobValidationModel createJobValidationModel = CreateNewCreateJobValidationModel();

            createJobValidationModel.JobCreateModel.Properties.Add("prop-3", "property 3");

            CreateJobValidator validator = new CreateJobValidator();

            //Act
            ValidationResult result = validator.Validate(createJobValidationModel);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public void CreateJobValidator_GivenValidModel_ValidIsTrue()
        {
            //Arrange
            CreateJobValidationModel createJobValidationModel = CreateNewCreateJobValidationModel();

            CreateJobValidator validator = new CreateJobValidator();

            //Act
            ValidationResult result = validator.Validate(createJobValidationModel);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }

        private static CreateJobValidationModel CreateNewCreateJobValidationModel()
        {
            JobCreateModel jobCreateModel = new JobCreateModel
            {
                JobDefinitionId = "job-def-1",
                Trigger = new Trigger
                {
                    EntityId = "spec-1"
                },
                SpecificationId = "spec-1",
                MessageBody = "body",
                Properties = new Dictionary<string, string>
                {
                    { "prop-1", "property 1" },
                    { "prop-2", "property 2" }
                },
                InvokerUserId = "authorId",
                InvokerUserDisplayName = "authorname"
            };

            JobDefinition jobDefinition = new JobDefinition
            {
                Id = "job-def-1",
                RequireEntityId = true,
                RequireSpecificationId = true,
                RequireMessageBody = true,
                RequireMessageProperties = new[] { "prop-1", "prop-2" }
            };

            return new CreateJobValidationModel
            {
                JobCreateModel = jobCreateModel,
                JobDefinition = jobDefinition
            };
        }

    }
}
