using CalculateFunding.Models.Policy;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Services.Policy.Models;
using CalculateFunding.Services.Policy.UnitTests;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Policy.UnitTests
{
    [TestClass]
    public class FundingTemplateValidationServiceTests
    {
        private const string fundingSchemaFolder = "funding";

        [TestMethod]
        public async Task ValidateFundingTemplate_GivenTemplateWithInvalidJson_ReturnsValidationResultWithErrors()
        {
            //Arrange
            string fundingTemplate = "invalid json schema";

            FundingTemplateValidationService fundingTemplateValidationService = CreateFundingTemplateValidationService();

            //Act
            FundingTemplateValidationResult result = await fundingTemplateValidationService.ValidateFundingTemplate(fundingTemplate);

            //Assert
            result
                .ValidationState
                .Errors
                .Should()
                .HaveCount(1);

            result
               .IsValid
               .Should()
               .BeFalse();
        }

        [TestMethod]
        public async Task ValidateFundingTemplate_GivenTemplateThatDoesNotContainASchemaVersionProperty_ReturnsValidationResultWithErrors()
        {
            //Arrange
            string fundingTemplate = "{}";

            FundingTemplateValidationService fundingTemplateValidationService = CreateFundingTemplateValidationService();

            //Act
            FundingTemplateValidationResult result = await fundingTemplateValidationService.ValidateFundingTemplate(fundingTemplate);

            //Assert
            result
                .ValidationState
                .Errors
                .Should()
                .HaveCount(1);

            result
               .ValidationState
               .Errors[0]
               .ErrorMessage
               .Should()
               .Be("Missing schema version from funding template.");

            result
               .IsValid
               .Should()
               .BeFalse();
        }

        [TestMethod]
        public async Task ValidateFundingTemplate_GivenTemplateThatDoesNotContainASchemaVersionValue_ReturnsValidationResultWithErrors()
        {
            //Arrange
            string fundingTemplate = "{ \"schemaVersion\" : \"\"}";

            FundingTemplateValidationService fundingTemplateValidationService = CreateFundingTemplateValidationService();

            //Act
            FundingTemplateValidationResult result = await fundingTemplateValidationService.ValidateFundingTemplate(fundingTemplate);

            //Assert
            result
                .ValidationState
                .Errors
                .Should()
                .HaveCount(1);

            result
               .ValidationState
               .Errors[0]
               .ErrorMessage
               .Should()
               .Be("Missing schema version from funding template.");

            result
               .IsValid
               .Should()
               .BeFalse();
        }

        [TestMethod]
        public async Task ValidateFundingTemplate_GivenTemplateWIthValidSchemaVersionButSchemaDoesNotExist_ReturnsValidationResultWithErrors()
        {
            //Arrange
            const string schemaVersion = "1.0";
            string fundingTemplate = $"{{ \"schemaVersion\" : \"{schemaVersion}\"}}";

            string blobName = $"{fundingSchemaFolder}/{schemaVersion}.json";

            IFundingSchemaRepository fundingSchemaRepository = CreateFundingSchemaRepository();
            fundingSchemaRepository
                .SchemaVersionExists(Arg.Is(blobName))
                .Returns(false);

            FundingTemplateValidationService fundingTemplateValidationService = CreateFundingTemplateValidationService(fundingSchemaRepository: fundingSchemaRepository);

            //Act
            FundingTemplateValidationResult result = await fundingTemplateValidationService.ValidateFundingTemplate(fundingTemplate);

            //Assert
            result
                .ValidationState
                .Errors
                .Should()
                .HaveCount(1);

            result
               .ValidationState
               .Errors[0]
               .ErrorMessage
               .Should()
               .Be($"A valid schema could not be found for schema version '{schemaVersion}'.");

            result
               .IsValid
               .Should()
               .BeFalse();
        }

        [TestMethod]
        public async Task ValidateFundingTemplate_GivenTemplateWIthValidSchemaVersionButTemplateDoesNotValidateAgainstTheSchema_ReturnsValidationResultWithErrors1()
        {
            //Arrange
            const string schemaVersion = "1.0";

            string fundingTemplate = CreateJsonFile("CalculateFunding.Services.Policy.Resources.LogicalModelTemplate.json");
            string fundingSchema = CreateJsonFile("CalculateFunding.Services.Policy.Resources.LogicalModel.json");

            string blobName = $"{fundingSchemaFolder}/{schemaVersion}.json";

            IFundingSchemaRepository fundingSchemaRepository = CreateFundingSchemaRepository();
            fundingSchemaRepository
                .SchemaVersionExists(Arg.Is(blobName))
                .Returns(true);
            fundingSchemaRepository
                .GetFundingSchemaVersion(Arg.Is(blobName))
                .Returns(fundingSchema);

            FundingTemplateValidationService fundingTemplateValidationService = CreateFundingTemplateValidationService(fundingSchemaRepository: fundingSchemaRepository);

            //Act
            FundingTemplateValidationResult result = await fundingTemplateValidationService.ValidateFundingTemplate(fundingTemplate);

            //Assert
            result
                .ValidationState
                .Errors
                .Should()
                .HaveCount(12);

            result
               .IsValid
               .Should()
               .BeFalse();
        }

        [TestMethod]
        public async Task ValidateFundingTemplate_GivenTemplateWithValidSchemaVersionButNoFundingPropertyToValidate_ReturnsValidationResultWithErrors()
        {
            //Arrange
            const string schemaVersion = "1.0";

            JSchemaGenerator generator = new JSchemaGenerator();

            JSchema schema = generator.Generate(typeof(TestClassWithNoFundingProperty));

            TestClassWithNoFundingProperty testClassWithNoFunding = new TestClassWithNoFundingProperty
            {
                Test = "Whatever",
                SchemaVersion = "1.0"
            };

            string fundingTemplate = JsonConvert.SerializeObject(testClassWithNoFunding);

            string fundingSchema = schema.ToString();

            string blobName = $"{fundingSchemaFolder}/{schemaVersion}.json";

            IFundingSchemaRepository fundingSchemaRepository = CreateFundingSchemaRepository();
            fundingSchemaRepository
                .SchemaVersionExists(Arg.Is(blobName))
                .Returns(true);
            fundingSchemaRepository
                .GetFundingSchemaVersion(Arg.Is(blobName))
                .Returns(fundingSchema);

            FundingTemplateValidationService fundingTemplateValidationService = CreateFundingTemplateValidationService(fundingSchemaRepository: fundingSchemaRepository);

            //Act
            FundingTemplateValidationResult result = await fundingTemplateValidationService.ValidateFundingTemplate(fundingTemplate);

            //Assert
            result
                .ValidationState
                .Errors[0]
                .ErrorMessage
                .Should()
                .Be("No funding property found");

            result
               .IsValid
               .Should()
               .BeFalse();
        }

        [TestMethod]
        public async Task ValidateFundingTemplate_GivenTemplateWIthValidSchemaVersionAndFundingPropertyButNoFundingStreamProperty_ReturnsValidationResultWithErrors()
        {
            //Arrange
            const string schemaVersion = "1.0";

            JSchemaGenerator generator = new JSchemaGenerator();

            JSchema schema = generator.Generate(typeof(TestClassWithFundingProperty));

            TestClassWithFundingProperty testClassWithFunding = new TestClassWithFundingProperty
            {
                SchemaVersion = "1.0",
                Funding = new { }
            };

            string fundingTemplate = JsonConvert.SerializeObject(testClassWithFunding);

            string fundingSchema = schema.ToString();

            string blobName = $"{fundingSchemaFolder}/{schemaVersion}.json";

            IFundingSchemaRepository fundingSchemaRepository = CreateFundingSchemaRepository();
            fundingSchemaRepository
                .SchemaVersionExists(Arg.Is(blobName))
                .Returns(true);
            fundingSchemaRepository
                .GetFundingSchemaVersion(Arg.Is(blobName))
                .Returns(fundingSchema);

            FundingTemplateValidationService fundingTemplateValidationService = CreateFundingTemplateValidationService(fundingSchemaRepository: fundingSchemaRepository);

            //Act
            FundingTemplateValidationResult result = await fundingTemplateValidationService.ValidateFundingTemplate(fundingTemplate);

            //Assert
            result
                .ValidationState
                .Errors[0]
                .ErrorMessage
                .Should()
                .Be("No funding stream property found");

            result
               .IsValid
               .Should()
               .BeFalse();
        }

        [TestMethod]
        public async Task ValidateFundingTemplate_GivenTemplateWIthValidSchemaVersionAndFundingPropertyButNoFundingStreamCodeProperty_ReturnsValidationResultWithErrors()
        {
            //Arrange
            const string schemaVersion = "1.0";

            JSchemaGenerator generator = new JSchemaGenerator();

            JSchema schema = generator.Generate(typeof(TestClassWithFundingProperty));

            TestClassWithFundingProperty testClassWithFunding = new TestClassWithFundingProperty
            {
                SchemaVersion = "1.0",
                Funding = new { fundingStream = new { } }
            };

            string fundingTemplate = JsonConvert.SerializeObject(testClassWithFunding);

            string fundingSchema = schema.ToString();

            string blobName = $"{fundingSchemaFolder}/{schemaVersion}.json";

            IFundingSchemaRepository fundingSchemaRepository = CreateFundingSchemaRepository();
            fundingSchemaRepository
                .SchemaVersionExists(Arg.Is(blobName))
                .Returns(true);
            fundingSchemaRepository
                .GetFundingSchemaVersion(Arg.Is(blobName))
                .Returns(fundingSchema);

            FundingTemplateValidationService fundingTemplateValidationService = CreateFundingTemplateValidationService(fundingSchemaRepository: fundingSchemaRepository);

            //Act
            FundingTemplateValidationResult result = await fundingTemplateValidationService.ValidateFundingTemplate(fundingTemplate);

            //Assert
            result
                .ValidationState
                .Errors[0]
                .ErrorMessage
                .Should()
                .Be("No funding stream code property found");

            result
               .IsValid
               .Should()
               .BeFalse();
        }

        [TestMethod]
        public async Task ValidateFundingTemplate_GivenTemplateWIthValidSchemaVersionAndFundingPropertyButNoTemplateVersionProperty_ReturnsValidationResultWithErrors()
        {
            //Arrange
            const string schemaVersion = "1.0";

            JSchemaGenerator generator = new JSchemaGenerator();

            JSchema schema = generator.Generate(typeof(TestClassWithFundingProperty));

            TestClassWithFundingProperty testClassWithFunding = new TestClassWithFundingProperty
            {
                SchemaVersion = "1.0",
                Funding = new { fundingStream = new { code = "" } }
            };

            string fundingTemplate = JsonConvert.SerializeObject(testClassWithFunding);

            string fundingSchema = schema.ToString();

            string blobName = $"{fundingSchemaFolder}/{schemaVersion}.json";

            IFundingSchemaRepository fundingSchemaRepository = CreateFundingSchemaRepository();
            fundingSchemaRepository
                .SchemaVersionExists(Arg.Is(blobName))
                .Returns(true);
            fundingSchemaRepository
                .GetFundingSchemaVersion(Arg.Is(blobName))
                .Returns(fundingSchema);

            FundingTemplateValidationService fundingTemplateValidationService = CreateFundingTemplateValidationService(fundingSchemaRepository: fundingSchemaRepository);

            //Act
            FundingTemplateValidationResult result = await fundingTemplateValidationService.ValidateFundingTemplate(fundingTemplate);

            //Assert
            result
                .ValidationState
                .Errors[0]
                .ErrorMessage
                .Should()
                .Be("No template version property found");

            result
               .IsValid
               .Should()
               .BeFalse();
        }

        [TestMethod]
        public async Task ValidateFundingTemplate_GivenTemplateWIthValidSchemaVersionAndFundingPropertyButCodeValueIsMissing_ReturnsValidationResultWithErrors()
        {
            //Arrange
            const string schemaVersion = "1.0";

            JSchemaGenerator generator = new JSchemaGenerator();

            JSchema schema = generator.Generate(typeof(TestClassWithFundingProperty));

            TestClassWithFundingProperty testClassWithFunding = new TestClassWithFundingProperty
            {
                SchemaVersion = "1.0",
                Funding = new { fundingStream = new { code = "", templateVersion = "1.2" } }
            };

            string fundingTemplate = JsonConvert.SerializeObject(testClassWithFunding);

            string fundingSchema = schema.ToString();

            string blobName = $"{fundingSchemaFolder}/{schemaVersion}.json";

            IFundingSchemaRepository fundingSchemaRepository = CreateFundingSchemaRepository();
            fundingSchemaRepository
                .SchemaVersionExists(Arg.Is(blobName))
                .Returns(true);
            fundingSchemaRepository
                .GetFundingSchemaVersion(Arg.Is(blobName))
                .Returns(fundingSchema);

            FundingTemplateValidationService fundingTemplateValidationService = CreateFundingTemplateValidationService(fundingSchemaRepository: fundingSchemaRepository);

            //Act
            FundingTemplateValidationResult result = await fundingTemplateValidationService.ValidateFundingTemplate(fundingTemplate);

            //Assert
            result
                .ValidationState
                .Errors[0]
                .ErrorMessage
                .Should()
                .Be("Funding stream id is missing from the template");

            result
               .IsValid
               .Should()
               .BeFalse();
        }

        [TestMethod]
        public async Task ValidateFundingTemplate_GivenTemplateWIthValidSchemaVersionAndFundingPropertyButFundingStreamDoesNotExist_ReturnsValidationResultWithErrors()
        {
            //Arrange
            const string schemaVersion = "1.0";

            JSchemaGenerator generator = new JSchemaGenerator();

            JSchema schema = generator.Generate(typeof(TestClassWithFundingProperty));

            TestClassWithFundingProperty testClassWithFunding = new TestClassWithFundingProperty
            {
                SchemaVersion = "1.0",
                Funding = new { fundingStream = new { code = "PES", templateVersion = "1.2" } }
            };

            string fundingTemplate = JsonConvert.SerializeObject(testClassWithFunding);

            string fundingSchema = schema.ToString();

            string blobName = $"{fundingSchemaFolder}/{schemaVersion}.json";

            IFundingSchemaRepository fundingSchemaRepository = CreateFundingSchemaRepository();
            fundingSchemaRepository
                .SchemaVersionExists(Arg.Is(blobName))
                .Returns(true);
            fundingSchemaRepository
                .GetFundingSchemaVersion(Arg.Is(blobName))
                .Returns(fundingSchema);

            IPolicyRepository policyRepository = CreatePolicyRepository();
            policyRepository
                .GetFundingStreamById(Arg.Is("PES"))
                .Returns((FundingStream)null);

            FundingTemplateValidationService fundingTemplateValidationService = CreateFundingTemplateValidationService(
                fundingSchemaRepository: fundingSchemaRepository,
                policyRepository: policyRepository);

            //Act
            FundingTemplateValidationResult result = await fundingTemplateValidationService.ValidateFundingTemplate(fundingTemplate);

            //Assert
            result
                .ValidationState
                .Errors[0]
                .ErrorMessage
                .Should()
                .Be("A funding stream could not be found for funding stream id 'PES'");

            result
               .IsValid
               .Should()
               .BeFalse();
        }

        [TestMethod]
        public async Task ValidateFundingTemplate_GivenTemplateWIthValidSchemaVersionAndFundingPropertyButTemplateVersionIsEmpty_ReturnsValidationResultWithErrors()
        {
            //Arrange
            const string schemaVersion = "1.0";

            FundingStream fundingStream = new FundingStream();

            JSchemaGenerator generator = new JSchemaGenerator();

            JSchema schema = generator.Generate(typeof(TestClassWithFundingProperty));

            TestClassWithFundingProperty testClassWithFunding = new TestClassWithFundingProperty
            {
                SchemaVersion = "1.0",
                Funding = new { fundingStream = new { code = "PES", templateVersion = "" } }
            };

            string fundingTemplate = JsonConvert.SerializeObject(testClassWithFunding);

            string fundingSchema = schema.ToString();

            string blobName = $"{fundingSchemaFolder}/{schemaVersion}.json";

            IFundingSchemaRepository fundingSchemaRepository = CreateFundingSchemaRepository();
            fundingSchemaRepository
                .SchemaVersionExists(Arg.Is(blobName))
                .Returns(true);
            fundingSchemaRepository
                .GetFundingSchemaVersion(Arg.Is(blobName))
                .Returns(fundingSchema);

            IPolicyRepository policyRepository = CreatePolicyRepository();
            policyRepository
                .GetFundingStreamById(Arg.Is("PES"))
                .Returns(fundingStream);

            FundingTemplateValidationService fundingTemplateValidationService = CreateFundingTemplateValidationService(
                fundingSchemaRepository: fundingSchemaRepository,
                policyRepository: policyRepository);

            //Act
            FundingTemplateValidationResult result = await fundingTemplateValidationService.ValidateFundingTemplate(fundingTemplate);

            //Assert
            result
                .ValidationState
                .Errors[0]
                .ErrorMessage
                .Should()
                .Be("Funding template version is missing from the template");

            result
               .IsValid
               .Should()
               .BeFalse();
        }

        [TestMethod]
        public async Task ValidateFundingTemplate_GivenTemplateIsValidAndValuesExtracted_ReturnsValidationResultWithNoErrors()
        {
            //Arrange
            const string schemaVersion = "1.0";

            FundingStream fundingStream = new FundingStream();

            JSchemaGenerator generator = new JSchemaGenerator();

            JSchema schema = generator.Generate(typeof(TestClassWithFundingProperty));

            TestClassWithFundingProperty testClassWithFunding = new TestClassWithFundingProperty
            {
                SchemaVersion = "1.0",
                Funding = new { fundingStream = new { code = "PES", templateVersion = "2.1" } }
            };

            string fundingTemplate = JsonConvert.SerializeObject(testClassWithFunding);

            string fundingSchema = schema.ToString();

            string blobName = $"{fundingSchemaFolder}/{schemaVersion}.json";

            IFundingSchemaRepository fundingSchemaRepository = CreateFundingSchemaRepository();
            fundingSchemaRepository
                .SchemaVersionExists(Arg.Is(blobName))
                .Returns(true);
            fundingSchemaRepository
                .GetFundingSchemaVersion(Arg.Is(blobName))
                .Returns(fundingSchema);

            IPolicyRepository policyRepository = CreatePolicyRepository();
            policyRepository
                .GetFundingStreamById(Arg.Is("PES"))
                .Returns(fundingStream);

            FundingTemplateValidationService fundingTemplateValidationService = CreateFundingTemplateValidationService(
                fundingSchemaRepository: fundingSchemaRepository,
                policyRepository: policyRepository);

            //Act
            FundingTemplateValidationResult result = await fundingTemplateValidationService.ValidateFundingTemplate(fundingTemplate);

            //Assert
            result
                .ValidationState
                .Errors
                .Should()
                .BeEmpty();

            result
                .Version
                .Should()
                .Be("2.1");

            result
                .FundingStreamId
                .Should()
                .Be("PES");

            result
                .IsValid
                .Should()
                .BeTrue();
        }

        private static FundingTemplateValidationService CreateFundingTemplateValidationService(
            IFundingSchemaRepository fundingSchemaRepository = null,
            IPolicyRepository policyRepository = null)
        {
            return new FundingTemplateValidationService(
                    fundingSchemaRepository ?? CreateFundingSchemaRepository(),
                    PolicyResilliencePoliciesTestHelper.GenerateTestPolicies(),
                    policyRepository ?? CreatePolicyRepository()
                );
        }

        private static IFundingSchemaRepository CreateFundingSchemaRepository()
        {
            return Substitute.For<IFundingSchemaRepository>();
        }

        private static IPolicyRepository CreatePolicyRepository()
        {
            return Substitute.For<IPolicyRepository>();
        }

        private static string CreateJsonFile(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                StreamReader reader = new StreamReader(stream);

                return reader.ReadToEnd();
            }
        }

        private class TestClassWithNoFundingProperty
        {
            [JsonProperty("test")]
            public string Test { get; set; }

            [JsonProperty("schemaVersion")]
            public string SchemaVersion { get; set; }
        }

        private class TestClassWithFundingProperty
        {
            [JsonProperty("schemaVersion")]
            public string SchemaVersion { get; set; }

            [JsonProperty("funding")]
            public dynamic Funding { get; set; }
        }
    }
}
