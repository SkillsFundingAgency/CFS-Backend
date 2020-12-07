using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CalculateFunding.Models.Code;
using CalculateFunding.Services.CodeMetadataGenerator.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.FeatureToggles;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.CodeMetadataGenerator.UnitTests
{
    [TestClass]
    public class ReflectionCodeMetadataGeneratorTests
    {
        private ICodeMetadataGeneratorService generator;

        [TestInitialize]
        public void SetUp()
        {
            generator = GetCodeGenerator();
        }

        [TestMethod]
        public void GetTypeInformation_SkipsCalculationsPropertyInFundingLinesClassInformation()
        {
            byte[] assemblyBytes = GetType()
                .Assembly
                .GetManifestResourceStream("CalculateFunding.Services.CodeMetadataGenerator.UnitTests.implementation.dll")
                .ReadAllBytes();

            TypeInformation[] codeContext = WhenTypeInformationForTheAssemblyIsCreated(assemblyBytes).ToArray();

            TypeInformation fundingLinesClass = codeContext.SingleOrDefault(_ => _.Name.EndsWith("FundingLines"));

            fundingLinesClass
                .Properties
                .Any(_ => _.Name.EndsWith("Calculations"))
                .Should()
                .BeFalse();
        }
        
        [TestMethod]
        public void GetTypeInformation_WhenAssemblyIsNullThenReturnsEmptyEnumerable()
        {
            // Act
            IEnumerable<TypeInformation> result = WhenTypeInformationForTheAssemblyIsCreated(null);

            // Assert
            result.Should().NotBeNull("Result should not be null");

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GetTypeInformation_WhenAssemblyIsZeroByteslThenReturnsEmptyEnumerable()
        {
            // Act
            IEnumerable<TypeInformation> result = WhenTypeInformationForTheAssemblyIsCreated(new byte[0]);

            // Assert
            result.Should().NotBeNull("Result should not be null");

            result.Should().BeEmpty();
        }

        [TestMethod]
        [Ignore("This test relies on an old assumption - calculations class")]
        public void GetTypeInformation_ReturnsCalculationObjectWithValidProperties()
        {
            // Act
            IEnumerable<TypeInformation> result = WhenTypeInformationForTheAssemblyIsCreated(GetEmptyDatasetExampleAssembly());

            // Assert
            result.Should().NotBeNull("Result should not be null");

            result.Should().Contain(c => c.Name == "Calculations");

            TypeInformation calculationType = result.Where(e => e.Name == "Calculations").FirstOrDefault();
            calculationType.Name.Should().Be("Calculations");
            calculationType.Description.Should().BeNull();
            calculationType.Type.Should().Be("Calculations");
        }

        [TestMethod]
        [Ignore("This test relies on an old assumption - calculations class")]
        public void GetTypeInformation_ReturnsCalculationObjectWithValidMethodProperties()
        {
            // Act
            IEnumerable<TypeInformation> result = WhenTypeInformationForTheAssemblyIsCreated(GetEmptyDatasetExampleAssembly());

            // Assert
            result.Should().NotBeNull("Result should not be null");

            result.Should().Contain(c => c.Name == "Calculations");

            TypeInformation calculationType = result.Where(e => e.Name == "Calculations").FirstOrDefault();
            calculationType.Methods.Should().NotBeEmpty();
        }

        [TestMethod]
        [Ignore("This test relies on an old assumption - calculations class")]
        public void GetTypeInformation_ReturnsCalculationObjectWithValidPrintMethod()
        {
            // Act
            IEnumerable<TypeInformation> result = WhenTypeInformationForTheAssemblyIsCreated(GetEmptyDatasetExampleAssembly());

            // Assert
            result.Should().NotBeNull("Result should not be null");

            result.Should().Contain(c => c.Name == "Calculations");

            TypeInformation calculationType = result.Where(e => e.Name == "Calculations").FirstOrDefault();
            calculationType.Methods.Should().ContainSingle(c => c.Name == "Print", "a single Print function should exist");

            MethodInformation printFunction = calculationType.Methods.Where(c => c.Name == "Print").SingleOrDefault();
            printFunction.Name.Should().Be("Print");

            printFunction.Parameters.Should().HaveCount(3);
            List<ParameterInformation> parameters = new List<ParameterInformation>(printFunction.Parameters);
            parameters[0].Name.Should().Be("value");
            parameters[0].Description.Should().Be("value");
            parameters[0].Type.Should().Be("T");

            parameters[1].Name.Should().Be("name");
            parameters[1].Description.Should().Be("name");
            parameters[1].Type.Should().Be("String");

            parameters[2].Name.Should().Be("rid");
            parameters[2].Description.Should().Be("rid");
            parameters[2].Type.Should().Be("String");

            printFunction.ReturnType.Should().BeNull();
        }

        [TestMethod]
        [Ignore("This test relies on an old assumption - calculations class")]
        public void GetTypeInformation_ReturnsCalculationObjectWithValidIntellisenseMethod()
        {
            // Act
            IEnumerable<TypeInformation> result = WhenTypeInformationForTheAssemblyIsCreated(GetEmptyDatasetExampleAssembly());

            // Assert
            result.Should().NotBeNull("Result should not be null");

            result.Should().Contain(c => c.Name == "Calculations");

            TypeInformation calculationType = result.Where(e => e.Name == "Calculations").FirstOrDefault();
            calculationType.Methods.Should().ContainSingle(c => c.Name == "Print", "a single Print function should exist");

            MethodInformation intellisenseMethod = calculationType.Methods.Where(c => c.Name == "Intellisense").SingleOrDefault();
            intellisenseMethod.Name.Should().Be("Intellisense");
            intellisenseMethod.Parameters.Should().HaveCount(0);

            intellisenseMethod.ReturnType.Should().Be("Decimal");
            intellisenseMethod.EntityId.Should().Be("24a85b7f-200a-4bd3-a1d0-ed883c4eb868");
        }

        [TestMethod]
        [Ignore("This test relies on an old assumption - requires old Calculations class")]
        public void GetTypeInformation_WithListDatasetsReturnsCalculationObjectWithValidProperties()
        {
            // Act
            IEnumerable<TypeInformation> result = WhenTypeInformationForTheAssemblyIsCreated(GetCalculationClassWithListDatasetsExampleAssembly());

            // Assert
            result.Should().NotBeNull("Result should not be null");

            result.Should().ContainSingle(c => c.Name == "Calculations");

            TypeInformation calculationType = result.Where(e => e.Name == "Calculations").FirstOrDefault();
            calculationType.Name.Should().Be("Calculations");
            calculationType.Description.Should().BeNull();
            calculationType.Type.Should().Be("Calculations");

            calculationType.Properties.Should().NotBeNull();

            PropertyInformation datasetsProperty = calculationType.Properties.Where(p => p.Name == "Datasets").SingleOrDefault();
            datasetsProperty.Should().NotBeNull();

            datasetsProperty.Type.Should().Be("Datasets");

            result.Should().ContainSingle(t => t.Name == "Datasets");
            TypeInformation datasetsType = result.Where(t => t.Name == "Datasets").SingleOrDefault();
            datasetsType.Should().NotBeNull();

            PropertyInformation firstPropertyOfDatasets = datasetsType.Properties.First();
            firstPropertyOfDatasets.Type.Should().Be("List(Of DemoTestAPTDatasetSchemaNoRequiredDataset)");
        }

        [TestMethod]
        [Ignore("This test relies on an old assumption - requires old Calculation class")]
        public void GetTypeInformation_WhenListDatasetsThenPropertyDescriptionShouldBeReturned()
        {
            // Act
            IEnumerable<TypeInformation> result = WhenTypeInformationForTheAssemblyIsCreated(GetCalculationClassWithListDescriptionsExampleAssembly());

            // Assert
            result.Should().NotBeNull("Result should not be null");

            result.Should().ContainSingle(c => c.Name == "Calculations");

            TypeInformation datasetType = result.Where(e => e.Name == "HighNeedsStudentNumbersDataset").FirstOrDefault();
            datasetType.Name.Should().Be("HighNeedsStudentNumbersDataset");
            datasetType.Description.Should().BeNull();
            datasetType.Type.Should().Be("HighNeedsStudentNumbersDataset");

            List<PropertyInformation> properties = new List<PropertyInformation>(datasetType.Properties);

            PropertyInformation ridProperty = properties[0];
            ridProperty.Should().NotBeNull("ridProperty should not be null");
            ridProperty.Name.Should().Be("Rid");
            ridProperty.FriendlyName.Should().Be("Rid");
            ridProperty.Description.Should().Be("Rid is the unique reference from The Store");

            PropertyInformation parentRidProperty = properties[1];
            parentRidProperty.Should().NotBeNull("parentRidProperty should not be null");
            parentRidProperty.Name.Should().Be("ParentRid");
            parentRidProperty.FriendlyName.Should().Be("Parent Rid");
            parentRidProperty.Description.Should().Be("The Rid of the parent provider (from The Store)");

            PropertyInformation highNeedsStudentsProperty = properties[11];
            highNeedsStudentsProperty.Should().NotBeNull("parentRidProperty should not be null");
            highNeedsStudentsProperty.Name.Should().Be("HighNeedsStudents1924");
            highNeedsStudentsProperty.FriendlyName.Should().Be("High Needs Students 19-24");
            highNeedsStudentsProperty.Description.Should().Be("Current year high needs students aged 19-24");

            properties.Should().HaveCount(22, "HighNeedsStudentNumbersDataset should contain expected number of properties");
        }

        [TestMethod]
        [Ignore("This test relies on an old assumption - update assembly for test")]
        public void GetTypeInformation_WhenListDatasetsThenDatasetPropertyDescriptionsShouldBeReturned()
        {
            // Act
            IEnumerable<TypeInformation> result = WhenTypeInformationForTheAssemblyIsCreated(GetCalculationClassWithListDescriptionsExampleAssembly());

            // Assert
            result.Should().NotBeNull("Result should not be null");

            result.Should().ContainSingle(c => c.Name == "Calculations");

            TypeInformation datasetType = result.Where(e => e.Name == "Datasets").FirstOrDefault();
            datasetType.Name.Should().Be("Datasets");
            datasetType.Description.Should().BeNull();
            datasetType.Type.Should().Be("Datasets");

            List<PropertyInformation> properties = new List<PropertyInformation>(datasetType.Properties);

            PropertyInformation firstDataset = properties[0];
            firstDataset.Should().NotBeNull("firstDataset should not be null");
            firstDataset.Name.Should().Be("ABTestDataset240301001");
            firstDataset.FriendlyName.Should().Be("AB Test Dataset 2403-01-001");
            firstDataset.Description.Should().Be("High Needs Student Numbers");

            PropertyInformation secondDataset = properties[1];
            secondDataset.Should().NotBeNull("firstDataset should not be null");
            secondDataset.Name.Should().Be("ABTestDataset24030020011");
            secondDataset.FriendlyName.Should().Be("AB Test Dataset 2403-002-0011");
            secondDataset.Description.Should().Be("High Needs Student Numbers");

            properties.Should().HaveCount(4, "Datasets should contain expected number of properties");
        }


        [TestMethod]
        [Ignore("This test relies on an old assumption - calculations class")]
        public void GetTypeInformation_WhenFeatureToggleIsOn_SetsIsCustomToTrue()
        {
            // Act
            IEnumerable<TypeInformation> result = WhenTypeInformationForTheAssemblyIsCreated(GetCalculationClassWithListDescriptionsExampleAssembly());

            // Assert
            result.Should().NotBeNull("Result should not be null");

            result.Should().ContainSingle(c => c.Name == "Calculations");

            TypeInformation datasetType = result.Where(e => e.Name == "Calculations").FirstOrDefault();
            datasetType.Name.Should().Be("Calculations");
            datasetType.Description.Should().BeNull();
            datasetType.Type.Should().Be("Calculations");

            List<MethodInformation> methods = new List<MethodInformation>(datasetType.Methods);

            methods.Should().HaveCount(5, "Calculations should contain expected number of methods");

            MethodInformation firstCalculation = methods.Where(m => m.Name == "ABHighNeedsCalc002").SingleOrDefault();
            firstCalculation.Should().NotBeNull("firstCalculation should not be null");
            firstCalculation.Name.Should().Be("ABHighNeedsCalc002");
            firstCalculation.FriendlyName.Should().Be("AB High Needs Calc 002");
            firstCalculation.Description.Should().Be("test");
            firstCalculation.IsCustom.Should().BeTrue();
        }

        [TestMethod]
        [Ignore("This test relies on an old assumption - update assembly for test")]
        public void GetTypeInformation_WhenListDatasetsThenEnsureDefaultTypesShouldBeReturned()
        {
            // Act
            IEnumerable<TypeInformation> result = WhenTypeInformationForTheAssemblyIsCreated(GetCalculationClassWithListDescriptionsExampleAssembly());

            // Assert
            result.Should().NotBeNull("Result should not be null");

            result.FirstOrDefault(m => m.Name == "Boolean").Should().NotBeNull();
            result.FirstOrDefault(m => m.Name == "Byte").Should().NotBeNull();
            result.FirstOrDefault(m => m.Name == "Char").Should().NotBeNull();
            result.FirstOrDefault(m => m.Name == "Date").Should().NotBeNull();
            result.FirstOrDefault(m => m.Name == "Decimal").Should().NotBeNull();
            result.FirstOrDefault(m => m.Name == "Double").Should().NotBeNull();
            result.FirstOrDefault(m => m.Name == "Integer").Should().NotBeNull();
            result.FirstOrDefault(m => m.Name == "Long").Should().NotBeNull();
            result.FirstOrDefault(m => m.Name == "Object").Should().NotBeNull();
            result.FirstOrDefault(m => m.Name == "SByte").Should().NotBeNull();
            result.FirstOrDefault(m => m.Name == "Short").Should().NotBeNull();
            result.FirstOrDefault(m => m.Name == "Single").Should().NotBeNull();
            result.FirstOrDefault(m => m.Name == "String").Should().NotBeNull();
            result.FirstOrDefault(m => m.Name == "UInteger").Should().NotBeNull();
            result.FirstOrDefault(m => m.Name == "ULong").Should().NotBeNull();
            result.FirstOrDefault(m => m.Name == "UShort").Should().NotBeNull();
        }

        [TestMethod]
        [Ignore("This test relies on an old assumption - update assembly for test")]
        public void GetTypeInformation_WhenListDatasetsThenEnsureKeywordsShouldBeReturned()
        {
            // Act
            IEnumerable<TypeInformation> result = WhenTypeInformationForTheAssemblyIsCreated(GetCalculationClassWithListDescriptionsExampleAssembly());

            // Assert
            result.Should().NotBeNull("Result should not be null");

            result.FirstOrDefault(m => m.Name == "If").Should().NotBeNull();
            result.FirstOrDefault(m => m.Name == "ElseIf").Should().NotBeNull();
            result.FirstOrDefault(m => m.Name == "EndIf").Should().NotBeNull();
            result.FirstOrDefault(m => m.Name == "Then").Should().NotBeNull();
            result.FirstOrDefault(m => m.Name == "If-Then").Should().NotBeNull();
            result.FirstOrDefault(m => m.Name == "If-Then-Else").Should().NotBeNull();
            result.FirstOrDefault(m => m.Name == "If-Then-ElseIf-Then").Should().NotBeNull();
        }

        [TestMethod]
        public void GetTypeInformation_WhenCompiledAssembly_EnsuresProviderPropertiesPresent()
        {
            IEnumerable<string> propertyNames = new[]
            {
                 "Name",  "DateOpened",  "ProviderType", "ProviderSubType",  "UKPRN", "URN", "UPIN", "DfeEstablishmentNumber", "EstablishmentNumber",
                 "LegalName", "Authority", "DateClosed", "LACode", "CrmAccountId", "NavVendorNo", "Status", "PhaseOfEducation", "LocalAuthorityName",
                 "CompaniesHouseNumber", "GroupIdNumber", "RscRegionName", "RscRegionCode", "GovernmentOfficeRegionName", "governmentOfficeRegionCode",
                 "DistrictName", "DistrictCode", "WardName", "WardCode", "CensusWardName", "CensusWardCode", "MiddleSuperOutputAreaName", "MiddleSuperOutputAreaCode",
                 "LowerSuperOutputAreaName", "LowerSuperOutputAreaCode", "ParliamentaryConstituencyName", "ParliamentaryConstituencyCode", "CountryCode", "CountryName"
            };

            // Act
            IEnumerable<TypeInformation> result = WhenTypeInformationForTheAssemblyIsCreated(GetTestNewProviderPropertiesAssembly());

            // Assert
            result.Should().NotBeNull("Result should not be null");

            result.FirstOrDefault(m => m.Name == "Provider").Should().NotBeNull();

            IEnumerable<PropertyInformation> providerProperties = result.FirstOrDefault(m => m.Name == "Provider").Properties;

            foreach (string propertyName in propertyNames)
            {
                providerProperties
                    .FirstOrDefault(m => m.Name == propertyName)
                    .Should()
                    .NotBeNull();
            }
        }

        [TestMethod]
        public void GetTypeInformation_WhenCompiledAssembly_EnsuresEnumsWithValuesPresent()
        {
            // Arrange
            string[] expectedEnumValues = new[] { "Type1", "Type2", "Type3" };

            // Act
            IEnumerable<TypeInformation> result = WhenTypeInformationForTheAssemblyIsCreated(GetCalculationsWithEnumsExampleAssembly());

            // Assert
            result.Should().NotBeNull("Result should not be null");

            TypeInformation enumTypeInformation = result.FirstOrDefault(t => t.Name == "TypeOfFundingOptions");
            enumTypeInformation.Should().NotBeNull();
            enumTypeInformation.EnumValues.Should().BeEquivalentTo(expectedEnumValues);
            enumTypeInformation.Type.Should().Be("CalculationContext+DSGCalculations+TypeOfFundingOptions");
        }

        [TestMethod]
        public void GetTypeInformation_WhenCompiledAssembly_EnsuresBooleanValueReturnMethodsPresent()
        {
            // Act
            IEnumerable<TypeInformation> result = WhenTypeInformationForTheAssemblyIsCreated(GetCalculationsWithEnumsExampleAssembly());

            // Assert
            result.Should().NotBeNull("Result should not be null");

            TypeInformation dsgCalcTypeInformation = result.FirstOrDefault(t => t.Name == "DSGCalculations");
            dsgCalcTypeInformation.Should().NotBeNull();
            MethodInformation booleanValueMethodInfo = dsgCalcTypeInformation.Methods.FirstOrDefault(x => x.Name == "Eligibilty");
            booleanValueMethodInfo.Should().NotBeNull();
            booleanValueMethodInfo.ReturnType.Should().Be("Nullable(Of System.Boolean)");
        }

        [TestMethod]
        public void GetTypeInformation_WhenCompiledAssembly_EnsuresFilteredMethodsNotPresent()
        {
            // Arrange
            string[] filteredMethodNames = new[]{
                                                    "ToString",
                                                    "GetHashCode",
                                                    "Equals",
                                                    "GetType",
                                                    "Initialise",
                                                    "GetTypeCode"
                                                };

            // Act
            IEnumerable<TypeInformation> result = WhenTypeInformationForTheAssemblyIsCreated(GetCalculationsWithEnumsExampleAssembly());

            // Assert
            result.Should().NotBeNull("Result should not be null");

            List<string> methodNames = result.SelectMany(x => (x.Methods ?? new List<MethodInformation>()).Select(m => m.Name)).ToList();
            methodNames.Should().NotContain(filteredMethodNames);
        }

        [TestMethod]
        public void GetTypeInformation_WhenCompiledAssembly_EnsuresFilteredPropertiesNotPresent()
        {
            // Arrange
            string[] filteredPropertyNames = new[]{
                                                    "Dictionary",
                                                    "DictionaryDecimalValues",
                                                    "DictionaryBooleanValues",
                                                    "DictionaryStringValues",
                                                    "FundingLineDictionary",
                                                    "FundingLineDictionaryValues"
                                                };

            // Act
            IEnumerable<TypeInformation> result = WhenTypeInformationForTheAssemblyIsCreated(GetCalculationsWithEnumsExampleAssembly());

            // Assert
            result.Should().NotBeNull("Result should not be null");

            List<string> propertyNames = result.SelectMany(x => (x.Properties ?? new List<PropertyInformation>()).Select(m => m.Name)).ToList();
            propertyNames.Should().NotContain(filteredPropertyNames);
        }

        private IEnumerable<TypeInformation> WhenTypeInformationForTheAssemblyIsCreated(byte[] assembly)
            => generator.GetTypeInformation(assembly);

        private static ICodeMetadataGeneratorService GetCodeGenerator()
        {
            return new ReflectionCodeMetadataGenerator();
        }

        private static IFeatureToggle CreateFeatureToggle()
        {
            return Substitute.For<IFeatureToggle>();
        }

        private byte[] GetEmptyDatasetExampleAssembly()
        {
            // Read this generated DLL as example input, it should be copied to the output directory to be read by the tests
            return File.ReadAllBytes(Path.Combine(Environment.CurrentDirectory, "out.dll.dat"));
        }

        private byte[] GetCalculationClassWithListDatasetsExampleAssembly()
        {
            // Read this generated DLL as example input, it should be copied to the output directory to be read by the tests
            return File.ReadAllBytes(Path.Combine(Environment.CurrentDirectory, "calculationsWithListDatasets.dll.dat"));
        }

        private byte[] GetCalculationClassWithListDescriptionsExampleAssembly()
        {
            // Read this generated DLL as example input, it should be copied to the output directory to be read by the tests
            return File.ReadAllBytes(Path.Combine(Environment.CurrentDirectory, "calculationsWithDescriptions.dll.dat"));
        }

        private byte[] GetTestNewProviderPropertiesAssembly()
        {
            // Read this generated DLL as example input, it should be copied to the output directory to be read by the tests
            return File.ReadAllBytes(Path.Combine(Environment.CurrentDirectory, "TestNewProviderProperties.dll.dat"));
        }

        private byte[] GetCalculationsWithEnumsExampleAssembly()
        {
            // Read this generated DLL as example input, it should be copied to the output directory to be read by the tests
            return File.ReadAllBytes(Path.Combine(Environment.CurrentDirectory, "calculationsWithEnums.dll.dat"));
        }
    }
}
