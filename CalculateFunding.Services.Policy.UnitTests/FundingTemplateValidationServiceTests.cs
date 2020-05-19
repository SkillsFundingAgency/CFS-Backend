using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using CalculateFunding.Models.Policy;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Services.Policy.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using NSubstitute;

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
                .Errors
                .Should()
                .HaveCount(1);

            result
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
                .Errors
                .Should()
                .HaveCount(1);

            result
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
                .Errors
                .Should()
                .HaveCount(1);

            result
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
                .Errors
                .Should()
                .HaveCount(11);

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

            JSchema schema = generator.Generate(typeof(TestTemplate_schema_1_0));

            TestTemplate_schema_1_0 testClassWithFunding = new TestTemplate_schema_1_0
            {
                SchemaVersion = "1.0",
                Funding = new { templateVersion = "1.2", fundingStream = new { code = "" } }
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
        public async Task ValidateFundingTemplate_GivenTemplateWithValidSchemaVersionAndFundingPropertyButFundingStreamDoesNotExist_ReturnsValidationResultWithErrors()
        {
            //Arrange
            const string schemaVersion = "1.0";

            JSchemaGenerator generator = new JSchemaGenerator();

            JSchema schema = generator.Generate(typeof(TestTemplate_schema_1_0));

            TestTemplate_schema_1_0 testClassWithFunding = new TestTemplate_schema_1_0
            {
                SchemaVersion = "1.0",
                Funding = new { templateVersion = "1.2", fundingStream = new { code = "PSG" } }
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
                .GetFundingStreamById(Arg.Is("PSG"))
                .Returns((FundingStream)null);

            FundingTemplateValidationService fundingTemplateValidationService = CreateFundingTemplateValidationService(
                fundingSchemaRepository: fundingSchemaRepository,
                policyRepository: policyRepository);

            //Act
            FundingTemplateValidationResult result = await fundingTemplateValidationService.ValidateFundingTemplate(fundingTemplate);

            //Assert
            result
                .Errors[0]
                .ErrorMessage
                .Should()
                .Be("A funding stream could not be found for funding stream id 'PSG'");

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

            JSchema schema = generator.Generate(typeof(TestTemplate_schema_1_0));

            TestTemplate_schema_1_0 testClassWithFunding = new TestTemplate_schema_1_0
            {
                SchemaVersion = "1.0",
                Funding = new { templateVersion = "", fundingStream = new { code = "PES" } }
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
        public async Task ValidateFundingTemplate_Schema_1_0_GivenTemplateIsValidAndValuesExtracted_ReturnsValidationResultWithNoErrors()
        {
            //Arrange
            const string schemaVersion = "1.0";

            FundingStream fundingStream = new FundingStream();

            JSchemaGenerator generator = new JSchemaGenerator();

            JSchema schema = generator.Generate(typeof(TestTemplate_schema_1_0));

            TestTemplate_schema_1_0 testClassWithFunding = new TestTemplate_schema_1_0
            {
                SchemaVersion = schemaVersion,
                Funding = new { templateVersion = "2.1", fundingStream = new { code = "PES" } }
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
                .Errors
                .Should()
                .BeEmpty();

            result
                .TemplateVersion
                .Should()
                .Be("2.1");

            result
                .SchemaVersion
                .Should()
                .Be("1.0");

            result
                .FundingStreamId
                .Should()
                .Be("PES");

            result
                .IsValid
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public async Task ValidateFundingTemplate_Schema_1_1_GivenTemplateIsValidAndValuesExtracted_ReturnsValidationResultWithNoErrors()
        {
            //Arrange
            const string schemaVersion = "1.1";

            FundingStream fundingStream = new FundingStream();

            JSchemaGenerator generator = new JSchemaGenerator();

            JSchema schema = generator.Generate(typeof(TestTemplate_schema_1_1));

            var template = new TestTemplate_schema_1_1
            {
                SchemaVersion = schemaVersion,
                FundingStreamTemplate = new { templateVersion = "56.4", fundingStream = new { code = "XXX" } }
            };

            string fundingTemplate = JsonConvert.SerializeObject(template);

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
                .GetFundingStreamById(Arg.Is("XXX"))
                .Returns(fundingStream);

            FundingTemplateValidationService fundingTemplateValidationService = CreateFundingTemplateValidationService(
                fundingSchemaRepository: fundingSchemaRepository,
                policyRepository: policyRepository);

            //Act
            FundingTemplateValidationResult result = await fundingTemplateValidationService.ValidateFundingTemplate(fundingTemplate);

            //Assert
            result
                .Errors
                .Should()
                .BeEmpty();

            result
                .TemplateVersion
                .Should()
                .Be("56.4");

            result
                .SchemaVersion
                .Should()
                .Be("1.1");

            result
                .FundingStreamId
                .Should()
                .Be("XXX");

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
                    PolicyResiliencePoliciesTestHelper.GenerateTestPolicies(),
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

            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            StreamReader reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }

        private class TestTemplate_schema_1_0
        {
            [JsonProperty("schemaVersion")]
            public string SchemaVersion { get; set; }

            [JsonProperty("funding")]
            public dynamic Funding { get; set; }
        }

        private class TestTemplate_schema_1_1
        {
            [JsonProperty("schemaVersion")]
            public string SchemaVersion { get; set; }

            [JsonProperty("fundingStreamTemplate")]
            public dynamic FundingStreamTemplate { get; set; }
        }
    }
}
