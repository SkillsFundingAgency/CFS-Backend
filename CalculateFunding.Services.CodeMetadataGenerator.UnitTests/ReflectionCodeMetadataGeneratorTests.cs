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
            calculationType.Description.Should().Be("Calculations");
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
            printFunction.Description.Should().Be("Print description from backend");

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
            intellisenseMethod.Description.Should().Be("Intellisense description from backend");
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
            calculationType.Description.Should().Be("Calculations");
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
    }
}
