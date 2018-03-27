using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CalculateFunding.Models.Code;
using CalculateFunding.Services.CodeMetadataGenerator.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.CodeMetadataGenerator.UnitTests
{
    [TestClass]
    public class ReflectionCodeMetadataGeneratorTests
    {
        [TestMethod]
        public void GetTypeInformation_WhenAssemblyIsNullThenReturnsEmptyEnumerable()
        {
            // Arrange
            ICodeMetadataGeneratorService generator = GetCodeGenerator();
            byte[] assembly = null;

            // Act
            IEnumerable<TypeInformation> result = generator.GetTypeInformation(assembly);

            // Assert
            result.Should().NotBeNull("Result should not be null");

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GetTypeInformation_WhenAssemblyIsZeroByteslThenReturnsEmptyEnumerable()
        {
            // Arrange
            ICodeMetadataGeneratorService generator = GetCodeGenerator();
            byte[] assembly = new byte[0];

            // Act
            IEnumerable<TypeInformation> result = generator.GetTypeInformation(assembly);

            // Assert
            result.Should().NotBeNull("Result should not be null");

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GetTypeInformation_ReturnsCalculationObjectWithValidProperties()
        {
            // Arrange
            ICodeMetadataGeneratorService generator = GetCodeGenerator();
            byte[] assembly = GetEmptyDatasetExampleAssembly();

            // Act
            IEnumerable<TypeInformation> result = generator.GetTypeInformation(assembly);

            // Assert
            result.Should().NotBeNull("Result should not be null");

            result.Should().Contain(c => c.Name == "Calculations");

            TypeInformation calculationType = result.Where(e => e.Name == "Calculations").FirstOrDefault();
            calculationType.Name.Should().Be("Calculations");
            calculationType.Description.Should().BeNull();
            calculationType.Type.Should().Be("Calculations");
        }

        [TestMethod]
        public void GetTypeInformation_ReturnsCalculationObjectWithValidMethodProperties()
        {
            // Arrange
            ICodeMetadataGeneratorService generator = GetCodeGenerator();
            byte[] assembly = GetEmptyDatasetExampleAssembly();

            // Act
            IEnumerable<TypeInformation> result = generator.GetTypeInformation(assembly);

            // Assert
            result.Should().NotBeNull("Result should not be null");

            result.Should().Contain(c => c.Name == "Calculations");

            TypeInformation calculationType = result.Where(e => e.Name == "Calculations").FirstOrDefault();
            calculationType.Methods.Should().NotBeEmpty();
        }

        [TestMethod]
        public void GetTypeInformation_ReturnsCalculationObjectWithValidPrintMethod()
        {
            // Arrange
            ICodeMetadataGeneratorService generator = GetCodeGenerator();
            byte[] assembly = GetEmptyDatasetExampleAssembly();

            // Act
            IEnumerable<TypeInformation> result = generator.GetTypeInformation(assembly);

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
        public void GetTypeInformation_ReturnsCalculationObjectWithValidIntellisenseMethod()
        {
            // Arrange
            ICodeMetadataGeneratorService generator = GetCodeGenerator();
            byte[] assembly = GetEmptyDatasetExampleAssembly();

            // Act
            IEnumerable<TypeInformation> result = generator.GetTypeInformation(assembly);

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
        public void GetTypeInformation_WithListDatasetsReturnsCalculationObjectWithValidProperties()
        {
            // Arrange
            ICodeMetadataGeneratorService generator = GetCodeGenerator();
            byte[] assembly = GetCalculationClassWithListDatasetsExampleAssembly();

            // Act
            IEnumerable<TypeInformation> result = generator.GetTypeInformation(assembly);

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
        public void GetTypeInformation_WhenListDatasetsThenPropertyDescriptionShouldBeReturned()
        {
            // Arrange
            ICodeMetadataGeneratorService generator = GetCodeGenerator();
            byte[] assembly = GetCalculationClassWithListDescriptionsExampleAssembly();

            // Act
            IEnumerable<TypeInformation> result = generator.GetTypeInformation(assembly);

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
        public void GetTypeInformation_WhenListDatasetsThenDatasetPropertyDescriptionsShouldBeReturned()
        {
            // Arrange
            ICodeMetadataGeneratorService generator = GetCodeGenerator();
            byte[] assembly = GetCalculationClassWithListDescriptionsExampleAssembly();

            // Act
            IEnumerable<TypeInformation> result = generator.GetTypeInformation(assembly);

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
        public void GetTypeInformation_WhenListDatasetsThenCalculationDetailsShouldBeReturned()
        {
            // Arrange
            ICodeMetadataGeneratorService generator = GetCodeGenerator();
            byte[] assembly = GetCalculationClassWithListDescriptionsExampleAssembly();

            // Act
            IEnumerable<TypeInformation> result = generator.GetTypeInformation(assembly);

            // Assert
            result.Should().NotBeNull("Result should not be null");

            result.Should().ContainSingle(c => c.Name == "Calculations");

            TypeInformation datasetType = result.Where(e => e.Name == "Calculations").FirstOrDefault();
            datasetType.Name.Should().Be("Calculations");
            datasetType.Description.Should().BeNull();
            datasetType.Type.Should().Be("Calculations");

            List<MethodInformation> methods = new List<MethodInformation>(datasetType.Methods);

            methods.Should().HaveCount(9, "Calculations should contain expected number of methods");

            MethodInformation firstCalculation = methods.Where(m=>m.Name== "ABHighNeedsCalc002").SingleOrDefault();
            firstCalculation.Should().NotBeNull("firstCalculation should not be null");
            firstCalculation.Name.Should().Be("ABHighNeedsCalc002");
            firstCalculation.FriendlyName.Should().Be("AB High Needs Calc 002");
            firstCalculation.Description.Should().Be("test");
        }

        private ICodeMetadataGeneratorService GetCodeGenerator()
        {
            return new ReflectionCodeMetadataGenerator();
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
    }
}
