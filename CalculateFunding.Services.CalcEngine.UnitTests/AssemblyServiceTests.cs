using CalculateFunding.Services.CalcEngine;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.CalcEngine.UnitTests;
using CalculateFunding.Services.Core;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calculator
{
    [TestClass]
    public class AssemblyServiceTests
    {
        const string specificationId = "spec1";
        const string eTag = "eTag1";

        private AssemblyService _assemblyService;
        
        private ILogger _mockLogger;
        private ISpecificationAssemblyProvider _mockSpecificationAssemblies;
        private ICalculatorResiliencePolicies _resiliencePolicies;
        private ICalculationsRepository _mockCalculationsRepository;

        [TestInitialize]
        public void Initialize()
        {
            _mockLogger = Substitute.For<ILogger>();
            _mockSpecificationAssemblies = Substitute.For<ISpecificationAssemblyProvider>();
            _resiliencePolicies = new CalculatorResiliencePolicies
            {
                CalculationsApiClient = Policy.NoOpAsync()
            };
            _mockCalculationsRepository = Substitute.For<ICalculationsRepository>();

            _assemblyService = new AssemblyService(_mockLogger, _mockSpecificationAssemblies, _resiliencePolicies, _mockCalculationsRepository);
        }

        [TestMethod]
        public async Task GetAssemblyForSpecification_GivenAssemblyFoundonSpecificationAssemblies__AssemblyContentReturnedFromSpecificationAssemblies()
        {
            _mockSpecificationAssemblies
                .GetAssembly(Arg.Is(specificationId), Arg.Is(eTag))
                .Returns(new MemoryStream(MockData.GetMockAssembly()));

            byte[] assemblyContent = await _assemblyService.GetAssemblyForSpecification(specificationId, eTag);

            assemblyContent.Should().BeEquivalentTo(MockData.GetMockAssembly());
        }

        [TestMethod]
        public async Task GetAssemblyForSpecification_GivenAssemblyFoundonSpecificationAssemblies_AssemblyContentReturnedFromRepository()
        {
            _mockCalculationsRepository
                .GetAssemblyBySpecificationId(Arg.Is(specificationId))
                .Returns(MockData.GetMockAssembly());

            byte[] assemblyContent = await _assemblyService.GetAssemblyForSpecification(specificationId, eTag);

            assemblyContent.Should().BeEquivalentTo(MockData.GetMockAssembly());
        }

        [TestMethod]
        public void GetAssemblyForSpecification_GivenAssemblyNotFoundon_ThrowsRetriableException()
        {
            _mockCalculationsRepository
                .GetAssemblyBySpecificationId(Arg.Is(specificationId))
                .Returns((byte[]) null);

            Func<Task> test = async () => await _assemblyService.GetAssemblyForSpecification(specificationId, eTag);

            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be($"Failed to get assembly for specification Id '{specificationId}'");
        }
    }
}
