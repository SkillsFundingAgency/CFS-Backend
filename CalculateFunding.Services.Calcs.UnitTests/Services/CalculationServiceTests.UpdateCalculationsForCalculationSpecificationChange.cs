using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Exceptions;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [TestMethod]
        public void UpdateCalculationsForCalculationSpecificationChange_GivenInvalidModel_LogsDoesNotSave()
        {
            //Arrange
            dynamic anyObject = new { something = 1 };

            string json = JsonConvert.SerializeObject(anyObject);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            CalculationService service = CreateCalculationService();

            //Act
            Func<Task> test = async () => await service.UpdateCalculationsForCalculationSpecificationChange(message);

            //Assert
            test
              .ShouldThrowExactly<InvalidModelException>();
        }

        [TestMethod]
        public void UpdateCalculationsForCalculationSpecificationChange_GivenModelButCurrentIsNull_LogsDoesNotSave()
        {
            //Arrange
            Models.Specs.CalculationVersionComparisonModel model = new Models.Specs.CalculationVersionComparisonModel();

            string json = JsonConvert.SerializeObject(model);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            CalculationService service = CreateCalculationService();

            //Act
            Func<Task> test = async () => await service.UpdateCalculationsForCalculationSpecificationChange(message);

            //Assert
            test
              .ShouldThrowExactly<InvalidModelException>();
        }

        [TestMethod]
        public void UpdateCalculationsForCalculationSpecificationChange_GivenModelButPreviousIsNull_LogsDoesNotSave()
        {
            //Arrange
            Models.Specs.CalculationVersionComparisonModel model = new Models.Specs.CalculationVersionComparisonModel
            {
                Current = new Models.Specs.Calculation()
            };

            string json = JsonConvert.SerializeObject(model);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            CalculationService service = CreateCalculationService();

            //Act
            Func<Task> test = async () => await service.UpdateCalculationsForCalculationSpecificationChange(message);

            //Assert
            test
              .ShouldThrowExactly<InvalidModelException>();
        }

        [TestMethod]
        public async Task UpdateCalculationsForCalculationSpecificationChange_GivenNoChanges_LogsAndReturns()
        {
            //Arrange
            Models.Specs.CalculationVersionComparisonModel model = new Models.Specs.CalculationVersionComparisonModel
            {
                CalculationId = "calc-id",
                SpecificationId = "spec-id",
                Current = new Models.Specs.Calculation(),
                Previous = new Models.Specs.Calculation()
            };

            string json = JsonConvert.SerializeObject(model);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            await service.UpdateCalculationsForCalculationSpecificationChange(message);

            //Assert
            logger
                .Received(1)
                .Information("No changes detected for calculation with id: '{calculationId}' on specification '{specificationId}'", Arg.Is("calc-id"), Arg.Is("spec-id"));
        }

        [TestMethod]
        public void UpdateCalculationsForCalculationSpecificationChange_GivenChangesButSpecificationCouldNotBeFound_ThrowsException()
        {
            //Arrange
            const string specificationId = "spec-id";

            Models.Specs.CalculationVersionComparisonModel model = new Models.Specs.CalculationVersionComparisonModel
            {
                Current = new Models.Specs.Calculation
                {
                    Name = "new name"
                },
                Previous = new Models.Specs.Calculation(),
                SpecificationId = specificationId
            };

            string json = JsonConvert.SerializeObject(model);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            Func<Task> test = async () => await service.UpdateCalculationsForCalculationSpecificationChange(message);

            //Assert
            test
              .ShouldThrowExactly<Exception>()
              .Which
              .Message
              .Should()
              .Be($"Specification could not be found for specification id : {specificationId}");
        }

        [TestMethod]
        public void UpdateCalculationsForCalculationSpecificationChange_GivenCalculationNotInCosmos_ThrowsException()
        {
            //Arrange
            const string specificationId = "spec-id";

            CalculationVersionComparisonModel model = new CalculationVersionComparisonModel
            {
                Current = new Models.Specs.Calculation
                {
                    Name = "name",
                    AllocationLine = new Reference { Id = "1" },
                    CalculationType = Models.Specs.CalculationType.Number
                },
                Previous = new Models.Specs.Calculation
                {
                    Name = "name",
                    AllocationLine = new Reference { Id = "1" },
                    CalculationType = Models.Specs.CalculationType.Funding
                },
                CalculationId = CalculationId,
                SpecificationId = specificationId
            };

            Models.Calcs.Calculation specCalculation = new Models.Calcs.Calculation
            {
                Name = "name",
                AllocationLine = new Reference { Id = "1" },
                CalculationType = Models.Calcs.CalculationType.Funding,
                Id = CalculationId
            };

            string json = JsonConvert.SerializeObject(model);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            SpecificationSummary specification = new SpecificationSummary
            {
                Name = "spec name"
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();

            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specification);

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns((Models.Calcs.Calculation)null);

            CalculationService service = CreateCalculationService(logger: logger, specificationRepository: specificationRepository, calculationsRepository: calculationsRepository);

            //Act
            Func<Task> test = async () => await service.UpdateCalculationsForCalculationSpecificationChange(message);

            //Assert
            test
              .ShouldThrowExactly<Exception>()
              .Which
              .Message
              .Should()
              .Be($"Calculation could not be found for calculation id : {CalculationId}");
        }

        [TestMethod]
        public async Task UpdateCalculationsForCalculationSpecificationChange_GivenCalcTypeChangedToNumber_RemovesAllocationLine()
        {
            //Arrange
            const string specificationId = "spec-id";

            CalculationVersionComparisonModel model = new CalculationVersionComparisonModel
            {
                Current = new Models.Specs.Calculation
                {
                    Name = "name",
                    AllocationLine = new Reference { Id = "1" },
                    CalculationType = Models.Specs.CalculationType.Number
                },
                Previous = new Models.Specs.Calculation
                {
                    Name = "name",
                    AllocationLine = new Reference { Id = "1" },
                    CalculationType = Models.Specs.CalculationType.Funding
                },
                CalculationId = CalculationId,
                SpecificationId = specificationId
            };

            Models.Calcs.Calculation specCalculation = new Models.Calcs.Calculation
            {
                Name = "name",
                AllocationLine = new Reference { Id = "1" },
                CalculationType = Models.Calcs.CalculationType.Funding,
                Id = CalculationId,
                CalculationSpecification = new Reference
                {
                    Id = "calc-spec-id",
                    Name = "calc spec name"
                },
                FundingPeriod = new Reference { Id = "fp1", Name = "fp 1" },
                Policies = new List<Reference> { new Reference { Id = "pol1", Name = "pol2" } },
                Current = new CalculationVersion
                {
                    SourceCode = "source code",
                    PublishStatus = PublishStatus.Published
                },
                SpecificationId = specificationId
            };

            string json = JsonConvert.SerializeObject(model);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            SpecificationSummary specification = new SpecificationSummary
            {
                Name = "spec name"
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();

            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specification);

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationByCalculationSpecificationId(Arg.Is(CalculationId))
                .Returns(specCalculation);

            CalculationService service = CreateCalculationService(logger: logger, specificationRepository: specificationRepository, calculationsRepository: calculationsRepository);

            //Act
            await service.UpdateCalculationsForCalculationSpecificationChange(message);

            //Assert
            specCalculation
                .CalculationType
                .Should()
                .Be(Models.Calcs.CalculationType.Number);

            specCalculation
               .AllocationLine
               .Should()
               .BeNull();
        }

        [TestMethod]
        public async Task UpdateCalculationsForCalculationSpecificationChange_GivenChanges_UpdatesCosmosAndSearch()
        {
            //Arrange
            const string specificationId = "spec-id";

            CalculationVersionComparisonModel model = new CalculationVersionComparisonModel
            {
                Current = new Models.Specs.Calculation
                {
                    Name = "name",
                    AllocationLine = new Reference { Id = "1" },
                    CalculationType = Models.Specs.CalculationType.Number
                },
                Previous = new Models.Specs.Calculation
                {
                    Name = "name",
                    AllocationLine = new Reference { Id = "1" },
                    CalculationType = Models.Specs.CalculationType.Funding
                },
                CalculationId = CalculationId,
                SpecificationId = specificationId,
            };

            Models.Calcs.Calculation specCalculation = new Models.Calcs.Calculation
            {
                Name = "name",
                AllocationLine = new Reference { Id = "1" },
                CalculationType = Models.Calcs.CalculationType.Funding,
                Id = CalculationId,
                FundingPeriod = new Reference { Id = "fp1", Name = "fp 1" },
                Policies = new List<Reference> { new Reference {  Id = "pol1", Name = "pol2" } },
                Current = new CalculationVersion
                {
                    SourceCode = "source code",
                    PublishStatus = PublishStatus.Published
                },
                CalculationSpecification = new Reference { Id = CalculationId, Name = "name" },
                SpecificationId = specificationId
            };

            string json = JsonConvert.SerializeObject(model);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            SpecificationSummary specification = new SpecificationSummary
            {
                Name = "spec name"
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();

            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specification);

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationByCalculationSpecificationId(Arg.Is(CalculationId))
                .Returns(specCalculation);

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            CalculationService service = CreateCalculationService(logger: logger, 
                specificationRepository: specificationRepository, calculationsRepository: calculationsRepository, searchRepository: searchRepository);

            //Act
            await service.UpdateCalculationsForCalculationSpecificationChange(message);

            //Assert
            await
                calculationsRepository
                .Received(1)
                .UpdateCalculations(Arg.Is<IEnumerable<Models.Calcs.Calculation>>(m => m.Count() == 1));

            await
                calculationsRepository
                .Received(1)
                .UpdateCalculations(Arg.Is<IEnumerable<Models.Calcs.Calculation>>(
                    m => m.First().Name == "name"  && 
                    m.First().AllocationLine == null
               ));

            await
                searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<CalculationIndex>>(m => m.Count() == 1));

        }
    }
}
