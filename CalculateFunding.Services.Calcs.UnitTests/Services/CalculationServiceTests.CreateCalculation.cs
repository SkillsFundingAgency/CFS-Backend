using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Exceptions;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using CalculateFunding.Services.Core;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [TestMethod]
        public async Task CreateCalculation_GivenNullCalculation_LogsDoesNotSave()
        {
            //Arrange
            Message message = new Message(new byte[0]);

            ICalculationsRepository repository = CreateCalculationsRepository();

            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(repository, logger);

            //Act
            await service.CreateCalculation(message);

            //Assert
            logger
                .Received(1)
                .Error("A null calculation was provided to CalculateFunding.Services.Calcs.CreateCalculation");

            await
                repository
                    .DidNotReceive()
                    .CreateDraftCalculation(Arg.Any<Calculation>());
        }

        [TestMethod]
        public void CreateCalculation_GivenInvalidCalculation_LogsDoesNotSave()
        {
            //Arrange
            dynamic anyObject = new { something = 1 };

            string json = JsonConvert.SerializeObject(anyObject);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ICalculationsRepository repository = CreateCalculationsRepository();

            ILogger logger = CreateLogger();

            ValidationResult validationResult = new ValidationResult(new[]{
                    new ValidationFailure("prop1", "any error")
                });

            IValidator<Calculation> validator = CreateCalculationValidator(validationResult);

            CalculationService service = CreateCalculationService(repository, logger, calcValidator: validator);

            //Act
            Func<Task> test = async () => await service.CreateCalculation(message);

            //Assert
            test
              .Should().ThrowExactly<InvalidModelException>();
        }

        [TestMethod]
        public async Task CreateCalculation_GivenValidCalculation_ButFailedToSave_DoesNotUpdateSearch()
        {
            //Arrange
            Calculation calculation = CreateCalculation();

            IEnumerable<Calculation> calculations = new[]
            {
                calculation
            };

            IEnumerable<Models.Specs.Calculation> calculationSpecifications = new[]
            {
                new Models.Specs.Calculation
                {
                    Id = calculation.CalculationSpecification.Id
                }
            };

            string json = JsonConvert.SerializeObject(calculation);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            message.UserProperties.Add("user-id", UserId);
            message.UserProperties.Add("user-name", Username);

            ICalculationsRepository repository = CreateCalculationsRepository();
            repository
                .CreateDraftCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.BadRequest);
            
            repository
                .GetCalculationsBySpecificationId(Arg.Is("any-spec-id"))
                .Returns(calculations);

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            specificationRepository
                .GetCalculationSpecificationsForSpecification(Arg.Is(calculation.SpecificationId))
                .Returns(calculationSpecifications);

            CalculationService service = CreateCalculationService(calculationsRepository: repository, logger: logger, searchRepository: searchRepository, specificationRepository: specificationRepository);

            //Act
            await service.CreateCalculation(message);

            //Assert
            logger
                .Received(1)
                .Error($"There was problem creating a new calculation with id {calculation.Id} in Cosmos Db with status code 400");

            await
               repository
                   .Received(1)
                   .CreateDraftCalculation(Arg.Is<Calculation>(m =>
                       m.Id == CalculationId &&
                       m.Current.PublishStatus == PublishStatus.Draft &&
                       m.Current.Author.Id == UserId &&
                       m.Current.Author.Name == Username &&
                       m.Current.Date.Date == DateTimeOffset.Now.Date &&
                       m.Current.Version == 1 &&
                       m.Current.DecimalPlaces == 6
                   ));

            await
                searchRepository
                    .DidNotReceive()
                    .Index(Arg.Any<List<CalculationIndex>>());
        }

        [TestMethod]
        public void CreateCalculation_CreatingCalculationWithTheExistingSpecificationId_ThrowsException()
        {
            //Arrange
            Calculation calculation = CreateCalculation();
            
            IEnumerable<Calculation> calculations = new[]
            {
                calculation
            };
            string json = JsonConvert.SerializeObject(calculation);

            IEnumerable<Models.Specs.Calculation> calculationSpecifications = new[]
            {
                new Models.Specs.Calculation
                {
                    Id = calculation.CalculationSpecification.Id
                }
            };

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            message.UserProperties.Add("user-id", UserId);
            message.UserProperties.Add("user-name", Username);

            
            ICalculationsRepository repository = CreateCalculationsRepository();
            repository
                .CreateDraftCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.Created);

            repository
                .GetCalculationsBySpecificationId(Arg.Is("any-spec-id"))
                .Returns(calculations);

            repository.GetCalculationByCalculationSpecificationId(Arg.Is("any-calc-id"))
                .Returns(calculation);

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            specificationRepository
               .GetCalculationSpecificationsForSpecification(Arg.Is(calculation.SpecificationId))
               .Returns(calculationSpecifications);

            CalculationService service = CreateCalculationService(calculationsRepository: repository, logger: logger, searchRepository: searchRepository, specificationRepository: specificationRepository);

            //Act
            Func<Task> test = async () => await service.CreateCalculation(message);

            //Assert
            test
              .Should().ThrowExactly<InvalidOperationException>()
              .WithMessage($"The calculation with the same id {calculation.CalculationSpecification.Id} has already been created");
        }

        [TestMethod]
        public void CreateCalculation_CreatingCalculationButAssociatedCalculationSpecificationNotFound_ThrowsException()
        {
            //Arrange
            Calculation calculation = CreateCalculation();

            IEnumerable<Calculation> calculations = new[]
            {
                calculation
            };
            string json = JsonConvert.SerializeObject(calculation);

            IEnumerable<Models.Specs.Calculation> calculationSpecifications = new[]
            {
                new Models.Specs.Calculation
                {
                    Id = "any-id"
                }
            };

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            message.UserProperties.Add("user-id", UserId);
            message.UserProperties.Add("user-name", Username);

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            specificationRepository
               .GetCalculationSpecificationsForSpecification(Arg.Is(calculation.SpecificationId))
               .Returns(calculationSpecifications);

            CalculationService service = CreateCalculationService(logger: logger, searchRepository: searchRepository, specificationRepository: specificationRepository);

            //Act
            Func<Task> test = async () => await service.CreateCalculation(message);

            //Assert
            test
              .Should().ThrowExactly<RetriableException>()
              .WithMessage($"A calculation specification was not found for calculation specification id '{calculation.CalculationSpecification.Id}'");
        }

        [TestMethod]
        public async Task CreateCalculation_GivenValidCalculation_AndSavesLogs()
        {
            //Arrange
            Calculation calculation = CreateCalculation();

            IEnumerable<Calculation> calculations = new[]
            {
                calculation
            };

            IEnumerable<Models.Specs.Calculation> calculationSpecifications = new[]
            {
                new Models.Specs.Calculation
                {
                    Id = calculation.CalculationSpecification.Id
                }
            };

            string json = JsonConvert.SerializeObject(calculation);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            message.UserProperties.Add("user-id", UserId);
            message.UserProperties.Add("user-name", Username);

            ICalculationsRepository repository = CreateCalculationsRepository();
            repository
                .CreateDraftCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.Created);

            repository
                .GetCalculationsBySpecificationId(Arg.Is("any-spec-id"))
                .Returns(calculations);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            specificationRepository
              .GetCalculationSpecificationsForSpecification(Arg.Is(calculation.SpecificationId))
              .Returns(calculationSpecifications);

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            Build build = new Build
            {
                SourceFiles = new List<SourceFile> { new SourceFile() }
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Models.Calcs.Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            CalculationService service = CreateCalculationService(
                calculationsRepository: repository, 
                logger: logger, 
                searchRepository: searchRepository, 
                specificationRepository: specificationRepository,
                sourceCodeService: sourceCodeService);

            //Act
            await service.CreateCalculation(message);

            //Assert
            logger
                .Received(1)
                .Information($"Calculation with id: {calculation.Id} was succesfully saved to Cosmos Db");

            await
               repository
                   .Received(1)
                   .CreateDraftCalculation(Arg.Is<Calculation>(m =>
                       m.Id == CalculationId &&
                       m.Current.PublishStatus == PublishStatus.Draft &&
                       m.Current.Author.Id == UserId &&
                       m.Current.Author.Name == Username &&
                       m.Current.Date.Date == DateTimeOffset.Now.Date &&
                       m.Current.Version == 1 &&
                       m.Current.DecimalPlaces == 6
                   ));

            await
                searchRepository
                    .Received(1)
                    .Index(Arg.Is<List<CalculationIndex>>(
                        m => m.First().Id == CalculationId &&
                        m.First().Name == "Test Calc Name" &&
                        m.First().CalculationSpecificationId == "any-calc-id" &&
                        m.First().CalculationSpecificationName == "Test Calc Name" &&
                        m.First().SpecificationId == "any-spec-id" &&
                        m.First().SpecificationName == "Test Spec Name" &&
                        m.First().FundingPeriodId == "18/19" &&
                        m.First().FundingPeriodName == "2018/2019" &&
                        m.First().AllocationLineId == "test-alloc-id" &&
                        m.First().AllocationLineName == "test-alloc-name" &&
                        m.First().PolicySpecificationIds.First() == "policy-id" &&
                        m.First().PolicySpecificationNames.First() == "policy-name" &&
                        m.First().FundingStreamId == "funding stream-id" &&
                        m.First().FundingStreamName == "funding-stream-name"
                  ));
        }

        [TestMethod]
        public async Task CreateCalculation_GivenValidCalculationWithNullFundingStream_AndSavesLogs()
        {
            //Arrange
            Calculation calculation = CreateCalculation();
            calculation.FundingStream = null;

            IEnumerable<Calculation> calculations = new[]
            {
                calculation
            };

            IEnumerable<Models.Specs.Calculation> calculationSpecifications = new[]
            {
                new Models.Specs.Calculation
                {
                    Id = calculation.CalculationSpecification.Id
                }
            };

            string json = JsonConvert.SerializeObject(calculation);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            message.UserProperties.Add("user-id", UserId);
            message.UserProperties.Add("user-name", Username);

            ICalculationsRepository repository = CreateCalculationsRepository();
            repository
                .CreateDraftCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.Created);

            repository
                .GetCalculationsBySpecificationId(Arg.Is("any-spec-id"))
                .Returns(calculations);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            specificationRepository
                 .GetCalculationSpecificationsForSpecification(Arg.Is(calculation.SpecificationId))
                 .Returns(calculationSpecifications);

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            Build build = new Build
            {
                SourceFiles = new List<SourceFile> { new SourceFile() }
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Models.Calcs.Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            CalculationService service = CreateCalculationService(
                calculationsRepository: repository, 
                logger: logger, searchRepository: 
                searchRepository, 
                specificationRepository: specificationRepository,
                sourceCodeService: sourceCodeService);

            //Act
            await service.CreateCalculation(message);

            //Assert
            logger
                .Received(1)
                .Information($"Calculation with id: {calculation.Id} was succesfully saved to Cosmos Db");

            await
               repository
                   .Received(1)
                   .CreateDraftCalculation(Arg.Is<Calculation>(m =>
                       m.Id == CalculationId &&
                       m.Current.PublishStatus == PublishStatus.Draft &&
                       m.Current.Author.Id == UserId &&
                       m.Current.Author.Name == Username &&
                       m.Current.Date.Date == DateTimeOffset.Now.Date &&
                       m.Current.Version == 1 &&
                       m.Current.DecimalPlaces == 6
                   ));

            await
                searchRepository
                    .Received(1)
                    .Index(Arg.Is<List<CalculationIndex>>(
                        m => m.First().Id == CalculationId &&
                        m.First().Name == "Test Calc Name" &&
                        m.First().CalculationSpecificationId == "any-calc-id" &&
                        m.First().CalculationSpecificationName == "Test Calc Name" &&
                        m.First().SpecificationId == "any-spec-id" &&
                        m.First().SpecificationName == "Test Spec Name" &&
                        m.First().FundingPeriodId == "18/19" &&
                        m.First().FundingPeriodName == "2018/2019" &&
                        m.First().FundingStreamId == string.Empty &&
                        m.First().FundingStreamName == "No funding stream set" &&
                        m.First().AllocationLineId == "test-alloc-id" &&
                        m.First().AllocationLineName == "test-alloc-name" &&
                        m.First().PolicySpecificationIds.First() == "policy-id" &&
                        m.First().PolicySpecificationNames.First() == "policy-name"
                  ));
        }

    }
}
