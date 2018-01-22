using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Exceptions;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Azure.ServiceBus;
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

namespace CalculateFunding.Services.Calcs.Services
{
    [TestClass]
    public class CalculationServiceTests
    {
        const string UserId = "8bcd2782-e8cb-4643-8803-951d715fc202";
        const string CalculationId = "3abc2782-e8cb-4643-8803-951d715fci23";
        const string Username = "test-user";

        [TestMethod]
        public async Task CreateCalculation_GivenNullCalculation_LogsDoesNotSave()
        {
            //Arrange
            Message message = new Message();

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
            Message message = new Message();

            dynamic anyObject = new { something = 1 };

            string json = JsonConvert.SerializeObject(anyObject);

            message.Body = Encoding.UTF8.GetBytes(json);


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
              .ShouldThrowExactly<InvalidModelException>();
        }

        [TestMethod]
        public async Task CreateCalculation_GivenValidCalculation_ButFailedToSave_DoesNotUpdateSearch()
        {
            //Arrange
            Message message = new Message();

            Calculation calculation = new Calculation { Id = CalculationId };

            string json = JsonConvert.SerializeObject(calculation);

            message.Body = Encoding.UTF8.GetBytes(json);

            message.UserProperties.Add("user-id", UserId);
            message.UserProperties.Add("user-name", Username);

            ICalculationsRepository repository = CreateCalculationsRepository();
            repository
                .CreateDraftCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.BadRequest);

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            CalculationService service = CreateCalculationService(repository, logger, searchRepository);

            //Act
            await service.CreateCalculation(message);

            //Assert
            logger
                .Received(1)
                .Error($"There was problem creating a new calculation with id {calculation.Id} in Cosmos Db with status code 400");

            await
               repository
                   .Received(1)
                   .CreateDraftCalculation(Arg.Is<Calculation>( m =>
                        m.Id == CalculationId &&
                        m.Current.PublishStatus == PublishStatus.Draft &&
                        m.Current.Author.Id == UserId &&
                        m.Current.Author.Name == Username &&
                        m.Current.Date.Date == DateTime.UtcNow.Date &&
                        m.Current.Version == 1 &&
                        m.Current.DecimalPlaces == 6
                   ));

            await
                searchRepository
                    .DidNotReceive()
                    .Index(Arg.Any<List<CalculationIndex>>());
        }

        [TestMethod]
        public async Task CreateCalculation_GivenValidCalculation_AndSavesLogs()
        {
            //Arrange
            Message message = new Message();

            Calculation calculation = new Calculation {
                Id = CalculationId,
                Name = "Test Calc Name",
                CalculationSpecification = new Reference
                {
                    Id = "any-calc-id",
                    Name = "Test Calc Name",
                },
                Specification = new Reference
                {
                    Id = "any-spec-id",
                    Name = "Test Spec Name",
                },
                Period = new Reference
                {
                    Id = "18/19",
                    Name = "2018/2019"
                },
                AllocationLine = new Reference
                {
                    Id = "test-alloc-id",
                    Name = "test-alloc-name"
                },
                Policies = new List<Reference>
                {
                    new Reference
                    {
                        Id = "policy-id",
                        Name = "policy-name"
                    }
                },
                Current = new CalculationVersion
                {
                    SourceCode = "source code",
                    PublishStatus = PublishStatus.Draft,
                },
                FundingStream = new Reference
                {
                    Id = "funding stream-id",
                    Name = "funding-stream-name"
                }
            };

            string json = JsonConvert.SerializeObject(calculation);

            message.Body = Encoding.UTF8.GetBytes(json);

            message.UserProperties.Add("user-id", UserId);
            message.UserProperties.Add("user-name", Username);

            ICalculationsRepository repository = CreateCalculationsRepository();
            repository
                .CreateDraftCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.Created);

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            CalculationService service = CreateCalculationService(repository, logger, searchRepository);

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
                       m.Current.Date.Date == DateTime.UtcNow.Date &&
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
                        m.First().PeriodId == "18/19" &&
                        m.First().PeriodName == "2018/2019" &&
                        m.First().AllocationLineId == "test-alloc-id" &&
                        m.First().AllocationLineName == "test-alloc-name" &&
                        m.First().PolicySpecificationIds.First() == "policy-id" &&
                        m.First().PolicySpecificationNames.First() == "policy-name"
                  ));
        }

        static CalculationService CreateCalculationService(ICalculationsRepository calculationsRepository = null, 
            ILogger logger = null, ISearchRepository<CalculationIndex> serachRepository = null, IValidator<Calculation> calcValidator = null)
        {
            return new CalculationService(calculationsRepository ?? CreateCalculationsRepository(), 
                logger ?? CreateLogger(), serachRepository ?? CreateSearchRepository(), calcValidator ?? CreateCalculationValidator());
        }

        static ICalculationsRepository CreateCalculationsRepository()
        {
            return Substitute.For<ICalculationsRepository>();
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static ISearchRepository<CalculationIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<CalculationIndex>>();
        }

        static IValidator<Calculation> CreateCalculationValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
                validationResult = new ValidationResult();

            IValidator<Calculation> validator = Substitute.For<IValidator<Calculation>>();

            validator
               .ValidateAsync(Arg.Any<Calculation>())
               .Returns(validationResult);

            return validator;
        }
    }
}
