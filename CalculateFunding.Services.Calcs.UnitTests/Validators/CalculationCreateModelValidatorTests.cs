using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;

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

            result.Errors
               .Should()
               .Contain(_ => _.ErrorMessage == "Null or empty calculation name provided.");
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

            result.Errors
                .Should()
                .Contain(_ => _.ErrorMessage == "'Specification Id' must not be empty.");
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


            result.Errors
                .Should()
                .Contain(_ => _.ErrorMessage == "Null or empty funding stream id provided.");
        }
        
        [TestMethod]
        public async Task ValidateAsync_WhenFundingStreamIdEmptyForAdditionalCalcs_ValidIsTrue()
        {
            //Arrange
            CalculationCreateModel model = CreateModel(CalculationType.Additional);
            model.FundingStreamId = string.Empty;

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(model.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, new SpecModel.SpecificationSummary()));

            CalculationCreateModelValidator validator = CreateValidator(specificationsApiClient: specificationsApiClient);

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

            result.Errors
               .Should()
               .Contain(_ => _.ErrorMessage == "Null value type was provided.");
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

            result.Errors
              .Should()
              .Contain(_ => _.ErrorMessage == "Null or empty source code provided.");

        }

        [TestMethod]
        public async Task ValidateAsync_WhenCalculationNameAlreadyExists_ValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();

            Calculation calculationWithSameName = new Calculation();

            ICalculationsRepository calculationsRepository = CreateCalculationRepository();
            calculationsRepository
                .GetCalculationBySpecificationIdAndCalculationName(Arg.Is(model.SpecificationId), Arg.Is(model.Name))
                .Returns(calculationWithSameName);

            CalculationCreateModelValidator validator = CreateValidator(calculationRepository: calculationsRepository);

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();
           
            result.Errors
              .Should()
              .Contain(_ => _.ErrorMessage == "A calculation already exists with the name: 'test calc' for this specification");
        }

        [DataTestMethod]
        [DataRow("\"")]
        public async Task ValidateAsync_WhenCalculationNameContainsNotAllowedCharacters_ValidIsFalse(string calculationNameNotAllowedCharacter)
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            model.Name += calculationNameNotAllowedCharacter;

            CalculationCreateModelValidator validator = CreateValidator();

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
        public async Task ValidateAsync_WhenCalculationSourceCodeNameAlreadyExists_ValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            string sourceCodeName = new VisualBasicTypeIdentifierGenerator().GenerateIdentifier(model.Name);

            Calculation calculationWithSameName = new Calculation();

            ICalculationsRepository calculationsRepository = CreateCalculationRepository();
            calculationsRepository
                .GetCalculationBySpecificationIdAndCalculationSourceCodeName(Arg.Is(model.SpecificationId), Arg.Is(sourceCodeName))
                .Returns(calculationWithSameName);

            CalculationCreateModelValidator validator = CreateValidator(calculationRepository: calculationsRepository);

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result.Errors
              .Should()
              .Contain(_ => _.ErrorMessage == $"A calculation already exists with the source code name: '{sourceCodeName}' for this specification");
        }

        [TestMethod]
        public async Task ValidateAsync_WhenSpecificationCanNotBeFound_ValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(model.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, null, null));

            CalculationCreateModelValidator validator = CreateValidator(specificationsApiClient: specificationsApiClient);

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();
         
            result.Errors
             .Should()
             .Contain(_ => _.ErrorMessage == "Failed to find specification for provided specification id.");
        }

        [TestMethod]
        public async Task ValidateAsync_WhenSpecificationDoesNotContainFundingStreamValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary
            {
                FundingStreams = new[] { new Reference() }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(model.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            CalculationCreateModelValidator validator = CreateValidator(specificationsApiClient: specificationsApiClient);

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();
            
            result.Errors
             .Should()
             .Contain(_ => _.ErrorMessage == "The funding stream id provided is not associated with the provided specification.");
        }

        [TestMethod]
        public async Task ValidateAsync_WhenSourceCodeDoesNotCompile__ValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            model.CalculationType = CalculationType.Additional;

            ICalculationsRepository calculationsRepository = CreateCalculationRepository();
            calculationsRepository
                .GetCalculationBySpecificationIdAndCalculationName(Arg.Is(model.SpecificationId), Arg.Is(model.Name))
                .Returns((Calculation)null);

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary
            {
                Name = "spec name",
                FundingStreams = new[] { new Reference(model.FundingStreamId, "funding stream name") }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(model.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

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

            CalculationCreateModelValidator validator = CreateValidator(
                calculationsRepository, previewService: previewService,specificationsApiClient: specificationsApiClient);

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
               .IsValid
               .Should()
               .BeFalse();

            result.Errors
             .Should()
             .Contain(_ => _.ErrorMessage == "There are errors in the source code provided");
        }

        [TestMethod]
        public async Task ValidateAsync_WhenSourceCodeDoesCompile__ValidIsTrue()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            model.CalculationType = CalculationType.Additional;

            ICalculationsRepository calculationsRepository = CreateCalculationRepository();
            calculationsRepository
                .GetCalculationBySpecificationIdAndCalculationName(Arg.Is(model.SpecificationId), Arg.Is(model.Name))
                .Returns((Calculation)null);

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary
            {
                Name = "spec name",
                FundingStreams = new[] { new Reference(model.FundingStreamId, "funding stream name") }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(model.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            IPreviewService previewService = CreatePreviewService();

            CalculationCreateModelValidator validator = CreateValidator(
                calculationsRepository, previewService: previewService, specificationsApiClient: specificationsApiClient);

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
               .IsValid
               .Should()
               .BeTrue();

            previewService
                .Received(1);


        }

        [TestMethod]
        public async Task ValidateAsync_WhenSourceCodeSkipCompile__ValidIsTrue()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
          
            ICalculationsRepository calculationsRepository = CreateCalculationRepository();
            calculationsRepository
                .GetCalculationBySpecificationIdAndCalculationName(Arg.Is(model.SpecificationId), Arg.Is(model.Name))
                .Returns((Calculation)null);

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary
            {
                Name = "spec name",
                FundingStreams = new[] { new Reference(model.FundingStreamId, "funding stream name") }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(model.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            IPreviewService previewService = CreatePreviewService();

            CalculationCreateModelValidator validator = CreateValidator(
                calculationsRepository, previewService: previewService, specificationsApiClient: specificationsApiClient);

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
               .IsValid
               .Should()
               .BeTrue();

            previewService
                .Received(0);


        }

        [TestMethod]
        public async Task ValidateAsync_WhenValidModel_ValidIsTrue()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();

           
            ICalculationsRepository calculationsRepository = CreateCalculationRepository();
            calculationsRepository
                .GetCalculationBySpecificationIdAndCalculationName(Arg.Is(model.SpecificationId), Arg.Is(model.Name))
                .Returns((Calculation)null);

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary
            {
                Name = "spec name",
                FundingStreams = new[] { new Reference(model.FundingStreamId, "funding stream name") }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(model.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            CalculationCreateModelValidator validator = CreateValidator(
                calculationsRepository, specificationsApiClient: specificationsApiClient);

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
            ISpecificationsApiClient specificationsApiClient = null)
        {
            return new CalculationCreateModelValidator(
                calculationRepository ?? CreateCalculationRepository(),
                previewService ?? CreatePreviewService(),
                specificationsApiClient ?? CreateSpecificationsApiClient(),
                CalcsResilienceTestHelper.GenerateTestPolicies());
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

        private static ISpecificationsApiClient CreateSpecificationsApiClient()
        {
            ISpecificationsApiClient specificationsApiClient =  Substitute.For<ISpecificationsApiClient>();

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                TemplateIds = new Dictionary<string, string> { { "fs-1", "2.2" } },
                FundingStreams = new List<Reference>()
                {
                    new Reference("fs-1", "PE and Sports"),
                },
            };

            specificationsApiClient
               .GetSpecificationSummaryById(Arg.Any<string>())
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            return specificationsApiClient;
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
