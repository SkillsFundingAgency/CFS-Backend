using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Exceptions;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.Compiler.Interfaces;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.CodeMetadataGenerator.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.CodeGeneration;
using Microsoft.Azure.ServiceBus;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;

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
              .ShouldThrowExactly<InvalidModelException>();
        }

        [TestMethod]
        public async Task CreateCalculation_GivenValidCalculation_ButFailedToSave_DoesNotUpdateSearch()
        {
            //Arrange

            Calculation calculation = new Calculation { Id = CalculationId };

            string json = JsonConvert.SerializeObject(calculation);


            Message message = new Message(Encoding.UTF8.GetBytes(json));

            message.UserProperties.Add("user-id", UserId);
            message.UserProperties.Add("user-name", Username);


            ICalculationsRepository repository = CreateCalculationsRepository();
            repository
                .CreateDraftCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.BadRequest);

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

            Calculation calculation = CreateCalculation();

            IEnumerable<Calculation> calculations = new[]
            {
                calculation
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

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            CalculationService service = CreateCalculationService(calculationsRepository: repository, logger: logger, searchRepository: searchRepository, specificationRepository: specificationRepository);

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
                       m.Current.DecimalPlaces == 6 &&
                       m.History.First().PublishStatus == PublishStatus.Draft &&
                       m.History.First().Author.Id == UserId &&
                       m.History.First().Author.Name == Username &&
                       m.History.First().Date.Date == DateTime.UtcNow.Date &&
                       m.History.First().Version == 1 &&
                       m.History.First().DecimalPlaces == 6
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

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            CalculationService service = CreateCalculationService(calculationsRepository: repository, logger: logger, searchRepository: searchRepository, specificationRepository: specificationRepository);

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
                       m.Current.DecimalPlaces == 6 &&
                       m.History.First().PublishStatus == PublishStatus.Draft &&
                       m.History.First().Author.Id == UserId &&
                       m.History.First().Author.Name == Username &&
                       m.History.First().Date.Date == DateTime.UtcNow.Date &&
                       m.History.First().Version == 1 &&
                       m.History.First().DecimalPlaces == 6
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

        [TestMethod]
        async public Task GetCalculationById_GivenNoCalculationIdWasprovided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            IActionResult result = await service.GetCalculationById(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No calculation Id was provided to GetCalculationById"));
        }

        [TestMethod]
        async public Task GetCalculationById_GivenCalculationIdWasProvidedButCalculationCouldNotBeFound_ReturnsNotFound()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns((Calculation)null);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository);

            //Act
            IActionResult result = await service.GetCalculationById(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"A calculation was not found for calculation id {CalculationId}"));
        }

        [TestMethod]
        async public Task GetCalculationById_GivenCalculationIdWasProvidedAndcalculationWasFound_ReturnsOK()
        {
            //Arrange
            Calculation calculation = new Calculation();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository);

            //Act
            IActionResult result = await service.GetCalculationById(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"A calculation was found for calculation id {CalculationId}"));
        }

        [TestMethod]
        public async Task GetCurrentCalculationsForSpecification_GivenNoCalculationIdWasProvided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            IActionResult result = await service.GetCurrentCalculationsForSpecification(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty specificationId provided");

            logger
                .Received(1)
                .Warning(Arg.Is("No specificationId was provided to GetCalculationsForSpecification"));
        }

        [TestMethod]
        public async Task GetCurrentCalculationsForSpecification_WhenCalculationsFoundInCache_ThenCalculationsReturnedFromCache()
        {
            //Arrange
            const string specificationId = "specid";

            List<CalculationCurrentVersion> calculations = new List<CalculationCurrentVersion>()
            {
                new CalculationCurrentVersion()
                {
                    Id ="one",
                    Name ="Calculation Name",
                    SourceCode = "Return 10",
                    Status = "Draft",
                    Author = new Reference("userId", "User Name"),
                    CalculationSpecification = new Reference("specId", "Calculation Specification ID"),
                    CalculationType = "Funding",
                    Version = 1,
                },
                new CalculationCurrentVersion()
                {
                    Id ="two",
                    Name ="Calculation Name Two",
                    SourceCode = "Return 50",
                    Status = "Published",
                    Author = new Reference("userId", "User Name"),
                    CalculationSpecification = new Reference("specId", "Calculation Specification ID"),
                    CalculationType = "Number",
                    Version = 5,
                }
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CalculationCurrentVersion>>($"{CacheKeys.CurrentCalculationsForSpecification}{specificationId}")
                .Returns(calculations);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await service.GetCurrentCalculationsForSpecification(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeAssignableTo<IEnumerable<CalculationCurrentVersion>>()
                .Which
                .ShouldAllBeEquivalentTo(new List<CalculationCurrentVersion>()
                {
                    new CalculationCurrentVersion()
                    {
                        Id ="one",
                        Name ="Calculation Name",
                        SourceCode = "Return 10",
                        Status = "Draft",
                        Author = new Reference("userId", "User Name"),
                        CalculationSpecification = new Reference("specId", "Calculation Specification ID"),
                        CalculationType = "Funding",
                        Version = 1,
                    },
                    new CalculationCurrentVersion()
                    {
                        Id ="two",
                        Name ="Calculation Name Two",
                        SourceCode = "Return 50",
                        Status = "Published",
                        Author = new Reference("userId", "User Name"),
                        CalculationSpecification = new Reference("specId", "Calculation Specification ID"),
                        CalculationType = "Number",
                        Version = 5,
                    }
                });

            await calculationsRepository
                .Received(0)
                .GetCalculationsBySpecificationId(Arg.Is(specificationId));

            await cacheProvider
                .Received(1)
                .GetAsync<List<CalculationCurrentVersion>>($"{CacheKeys.CurrentCalculationsForSpecification}{specificationId}");
        }

        [TestMethod]
        async public Task GetCurrentCalculationsForSpecification_WhenCalculationsNotFoundInCache_ThenCalculationsReturnedFromRepository()
        {
            //Arrange
            const string specificationId = "specid";

            DateTime calc1DateTime = DateTime.UtcNow;
            DateTime calc2DateTime = DateTime.UtcNow;

            List<Calculation> calculations = new List<Calculation>()
            {
                new Calculation()
                {
                    Id ="one",
                    Name ="Calculation Name",
                    Current = new CalculationVersion()
                    {
                        SourceCode = "Return 10",
                        PublishStatus = PublishStatus.Draft,
                        Author = new Reference("userId", "User Name"),
                        Version = 1,
                        Date = calc1DateTime,
                    },
                    CalculationSpecification = new Reference("specId", "Calculation Specification ID"),
                    CalculationType = CalculationType.Funding,
                    FundingPeriod = new Reference("fp1", "Funding Period 1"),
                    SpecificationId = specificationId,
                },
                new Calculation()
                {
                    Id ="two",
                    Name ="Calculation Name Two",
                    Current = new CalculationVersion()
                    {
                        SourceCode = "Return 50",
                        PublishStatus = PublishStatus.Published,
                        Author = new Reference("userId", "User Name"),
                        Version = 5,
                        Date= calc2DateTime,
                    },
                    CalculationSpecification = new Reference("specId", "Calculation Specification ID"),
                    CalculationType = CalculationType.Number,
                    FundingPeriod = new Reference("fp2", "Funding Period Two"),
                    SpecificationId = specificationId,
                }
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calculations);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CalculationCurrentVersion>>($"{CacheKeys.CurrentCalculationsForSpecification}{specificationId}")
                .Returns((List<CalculationCurrentVersion>)null);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await service.GetCurrentCalculationsForSpecification(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeAssignableTo<IEnumerable<CalculationCurrentVersion>>()
                .Which
                .ShouldAllBeEquivalentTo(new List<CalculationCurrentVersion>()
                {
                    new CalculationCurrentVersion()
                    {
                        Id ="one",
                        Name ="Calculation Name",
                        SourceCode = "Return 10",
                        Status = "Draft",
                        Author = new Reference("userId", "User Name"),
                        CalculationSpecification = new Reference("specId", "Calculation Specification ID"),
                        CalculationType = "Funding",
                        Version = 1,
                        FundingPeriodId = "fp1",
                        FundingPeriodName = "Funding Period 1",
                        Date = calc1DateTime,
                        SpecificationId = specificationId,
                    },
                    new CalculationCurrentVersion()
                    {
                        Id ="two",
                        Name ="Calculation Name Two",
                        SourceCode = "Return 50",
                        Status = "Published",
                        Author = new Reference("userId", "User Name"),
                        CalculationSpecification = new Reference("specId", "Calculation Specification ID"),
                        CalculationType = "Number",
                        Version = 5,
                        FundingPeriodId = "fp2",
                        FundingPeriodName = "Funding Period Two",
                        Date = calc2DateTime,
                        SpecificationId = specificationId,
                    }
                });

            await calculationsRepository
                .Received(1)
                .GetCalculationsBySpecificationId(Arg.Is(specificationId));

            await cacheProvider
                .Received(1)
                .GetAsync<List<CalculationCurrentVersion>>($"{CacheKeys.CurrentCalculationsForSpecification}{specificationId}");
        }

        [TestMethod]
        async public Task GetCurrentCalculationsForSpecification_WhenEmptyListOfCalculationsFoundInCache_ThenCalculationsReturned()
        {
            //Arrange
            const string specificationId = "specid";

            List<CalculationCurrentVersion> calculations = new List<CalculationCurrentVersion>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CalculationCurrentVersion>>($"{CacheKeys.CurrentCalculationsForSpecification}{specificationId}")
                .Returns(calculations);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await service.GetCurrentCalculationsForSpecification(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeAssignableTo<IEnumerable<CalculationCurrentVersion>>()
                .Which
                .Should()
                .HaveCount(calculations.Count);

            await calculationsRepository
                .Received(0)
                .GetCalculationsBySpecificationId(Arg.Is(specificationId));
        }

        [TestMethod]
        public async Task GetCalculationSummariesForSpecification_GivenNoCalculationIdWasProvided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            IActionResult result = await service.GetCalculationSummariesForSpecification(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty specificationId provided");

            logger
                .Received(1)
                .Warning(Arg.Is("No specificationId was provided to GetCalculationSummariesForSpecification"));
        }

        [TestMethod]
        public async Task GetCalculationSummariesForSpecification_WhenCalculationsFoundInCache_ThenCalculationsReturnedFromCache()
        {
            //Arrange
            const string specificationId = "specid";

            List<CalculationSummaryModel> calculations = new List<CalculationSummaryModel>()
            {
                new CalculationSummaryModel()
                {
                    Id ="one",
                    Name ="Calculation Name",
                    CalculationType = CalculationType.Funding
                },
                new CalculationSummaryModel()
                {
                    Id ="two",
                    Name ="Calculation Name Two",
                    CalculationType =  CalculationType.Number,
                }
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CalculationSummaryModel>>($"{CacheKeys.CalculationsSummariesForSpecification}{specificationId}")
                .Returns(calculations);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository, cacheProvider: cacheProvider);

            // Act
            IActionResult result = await service.GetCalculationSummariesForSpecification(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeAssignableTo<IEnumerable<CalculationSummaryModel>>()
                .Which
                .ShouldAllBeEquivalentTo(new List<CalculationSummaryModel>()
                {
                    new CalculationSummaryModel()
                    {
                        Id ="one",
                        Name ="Calculation Name",
                        CalculationType = CalculationType.Funding
                    },
                    new CalculationSummaryModel()
                    {
                        Id ="two",
                        Name ="Calculation Name Two",
                        CalculationType =  CalculationType.Number,
                    }
                });

            await calculationsRepository
                .Received(0)
                .GetCalculationsBySpecificationId(Arg.Is(specificationId));

            await cacheProvider
                .Received(1)
                .GetAsync<List<CalculationSummaryModel>>($"{CacheKeys.CalculationsSummariesForSpecification}{specificationId}");
        }

        [TestMethod]
        async public Task GetCalculationSummariesForSpecification_WhenCalculationsNotFoundInCache_ThenCalculationsReturnedFromRepository()
        {
            // Arrange
            const string specificationId = "specid";

            DateTime calc1DateTime = DateTime.UtcNow;
            DateTime calc2DateTime = DateTime.UtcNow;

            List<Calculation> calculations = new List<Calculation>()
            {
                new Calculation()
                {
                    Id ="one",
                    Name ="Calculation Name",
                    Current = new CalculationVersion()
                    {
                        SourceCode = "Return 10",
                        PublishStatus = PublishStatus.Draft,
                        Author = new Reference("userId", "User Name"),
                        Version = 1,
                        Date = calc1DateTime,
                    },
                    CalculationSpecification = new Reference("specId", "Calculation Specification ID"),
                    CalculationType = CalculationType.Funding,
                    FundingPeriod = new Reference("fp1", "Funding Period 1"),
                    SpecificationId = specificationId,
                },
                new Calculation()
                {
                    Id ="two",
                    Name ="Calculation Name Two",
                    Current = new CalculationVersion()
                    {
                        SourceCode = "Return 50",
                        PublishStatus = PublishStatus.Published,
                        Author = new Reference("userId", "User Name"),
                        Version = 5,
                        Date= calc2DateTime,
                    },
                    CalculationSpecification = new Reference("specId", "Calculation Specification ID"),
                    CalculationType = CalculationType.Number,
                    FundingPeriod = new Reference("fp2", "Funding Period Two"),
                    SpecificationId = specificationId,
                }
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calculations);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CalculationSummaryModel>>($"{CacheKeys.CalculationsSummariesForSpecification}{specificationId}")
                .Returns((List<CalculationSummaryModel>)null);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository, cacheProvider: cacheProvider);

            // Act
            IActionResult result = await service.GetCalculationSummariesForSpecification(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeAssignableTo<IEnumerable<CalculationSummaryModel>>()
                .Which
                .ShouldAllBeEquivalentTo(new List<CalculationSummaryModel>()
                {
                    new CalculationSummaryModel()
                    {
                        Id ="one",
                        Name ="Calculation Name",
                        CalculationType = CalculationType.Funding
                    },
                    new CalculationSummaryModel()
                    {
                        Id ="two",
                        Name ="Calculation Name Two",
                        CalculationType =  CalculationType.Number,
                    }
                });

            await calculationsRepository
                .Received(1)
                .GetCalculationsBySpecificationId(Arg.Is(specificationId));

            await cacheProvider
                .Received(1)
                .GetAsync<List<CalculationSummaryModel>>($"{CacheKeys.CalculationsSummariesForSpecification}{specificationId}");
        }

        [TestMethod]
        async public Task GetCalculationSummariesForSpecification_WhenEmptyListOfCalculationsFoundInCache_ThenCalculationsReturned()
        {
            // Arrange
            const string specificationId = "specid";

            List<CalculationSummaryModel> calculations = new List<CalculationSummaryModel>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CalculationSummaryModel>>($"{CacheKeys.CalculationsSummariesForSpecification}{specificationId}")
                .Returns(calculations);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await service.GetCalculationSummariesForSpecification(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeAssignableTo<IEnumerable<CalculationSummaryModel>>()
                .Which
                .Should()
                .HaveCount(calculations.Count);

            await calculationsRepository
                .Received(0)
                .GetCalculationsBySpecificationId(Arg.Is(specificationId));
        }

        [TestMethod]
        async public Task GetCalculationSummariesForSpecification_WhenCalculationsNotFoundInCacheAndResponseFromRepositoryIsNull_ThenErrorReturned()
        {
            // Arrange
            const string specificationId = "specid";

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns((IEnumerable<Calculation>)null);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CalculationSummaryModel>>($"{CacheKeys.CalculationsSummariesForSpecification}{specificationId}")
                .Returns((List<CalculationSummaryModel>)null);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository, cacheProvider: cacheProvider);

            // Act
            IActionResult result = await service.GetCalculationSummariesForSpecification(request);

            // Assert
            result
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be("Calculations from repository returned null");

            await calculationsRepository
                .Received(1)
                .GetCalculationsBySpecificationId(Arg.Is(specificationId));

            await cacheProvider
                .Received(1)
                .GetAsync<List<CalculationSummaryModel>>($"{CacheKeys.CalculationsSummariesForSpecification}{specificationId}");

            logger
                .Received(1)
                .Warning($"Calculations from repository returned null for specification ID of '{specificationId}'");
        }

        [TestMethod]
        async public Task GetCalculationHistory_GivenNoCalculationIdWasProvided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            IActionResult result = await service.GetCalculationHistory(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No calculation Id was provided to GetCalculationHistory"));
        }

        [TestMethod]
        async public Task GetCalculationHistory_GivenCalculationIdWasProvidedButHistoryWasNull_ReturnsNotFound()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetVersionHistory(Arg.Is(CalculationId))
                .Returns((IEnumerable<CalculationVersion>)null);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository);

            //Act
            IActionResult result = await service.GetCalculationHistory(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"A calculation was not found for calculation id {CalculationId}"));
        }

        [TestMethod]
        async public Task GetCalculationHistory_GivenCalculationIdWasProvided_ReturnsOK()
        {
            //Arrange
            IEnumerable<CalculationVersion> versions = new List<CalculationVersion>
            {
                new CalculationVersion(),
                new CalculationVersion()
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetVersionHistory(Arg.Is(CalculationId))
                .Returns(versions);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository);

            //Act
            IActionResult result = await service.GetCalculationHistory(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public async Task GetCalculationCurrentVersion_GivenNoCalculationIdprovided_returnsBadrequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            IActionResult result = await service.GetCalculationCurrentVersion(request);

            //Assert
            logger
                .Received(1)
                .Error("No calculation Id was provided to GetCalculationCurrentVersion");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        async public Task GetCalculationCurrentVersion_GivencalculationWasNotFound_ReturnsNotFound()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns((Calculation)null);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository);

            //Act
            IActionResult result = await service.GetCalculationCurrentVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"A calculation was not found for calculation id {CalculationId}"));
        }

        [TestMethod]
        async public Task GetCalculationCurrentVersion_GivencalculationWasFoundWithNoCurrent_ReturnsNotFound()
        {
            //Arrange
            Calculation calculation = new Calculation();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository);

            //Act
            IActionResult result = await service.GetCalculationCurrentVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"A current calculation was not found for calculation id {CalculationId}"));
        }

        [TestMethod]
        async public Task GetCalculationCurrentVersion_GivenCalculationExistsAndWasWasFoundInCache_ReturnsOK()
        {
            //Arrange
            const string specificationId = "specId";
            DateTime lastModifiedDate = DateTime.UtcNow;

            CalculationCurrentVersion calculation = new CalculationCurrentVersion
            {
                Author = new Reference(UserId, Username),
                Date = lastModifiedDate,
                SourceCode = "source code",
                Version = 1,
                Name = "any name",
                Id = CalculationId,
                CalculationSpecification = new Reference("any name", "any-id"),
                FundingPeriodName = "2018/2019",
                FundingPeriodId = "18/19",
                CalculationType = "Number",
                SpecificationId = specificationId,
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();

            ICacheProvider cacheProvider = CreateCacheProvider();

            cacheProvider
                .GetAsync<CalculationCurrentVersion>(Arg.Is($"{CacheKeys.CurrentCalcluation}{CalculationId}"))
                .Returns(calculation);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await service.GetCalculationCurrentVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .ShouldBeEquivalentTo(new CalculationCurrentVersion
                {
                    Author = new Reference(UserId, Username),
                    Date = lastModifiedDate,
                    SourceCode = "source code",
                    Version = 1,
                    Name = "any name",
                    Id = CalculationId,
                    CalculationSpecification = new Reference("any name", "any-id"),
                    FundingPeriodName = "2018/2019",
                    FundingPeriodId = "18/19",
                    CalculationType = "Number",
                    SpecificationId = specificationId,
                });

            await calculationsRepository
                .Received(0)
                .GetCalculationById(Arg.Any<string>());

            await cacheProvider
                .Received(1)
                .GetAsync<CalculationCurrentVersion>(Arg.Is($"{CacheKeys.CurrentCalcluation}{CalculationId}"));
        }

        [TestMethod]
        async public Task GetCalculationCurrentVersion_GivenCalculationExistsAndWasWasNotFoundInCache_ReturnsOK()
        {
            //Arrange
            const string specificationId = "specId";
            DateTime lastModifiedDate = DateTime.UtcNow;

            Calculation calculation = new Calculation
            {
                SpecificationId = specificationId,
                Current = new CalculationVersion
                {
                    Author = new Reference(UserId, Username),
                    Date = lastModifiedDate,
                    PublishStatus = PublishStatus.Draft,
                    SourceCode = "source code",
                    Version = 1
                },
                Name = "any name",
                Id = CalculationId,
                CalculationSpecification = new Reference("any name", "any-id"),
                FundingPeriod = new Reference("18/19", "2018/2019"),
                CalculationType = CalculationType.Number
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
               .GetAsync<CalculationCurrentVersion>(Arg.Is($"{CacheKeys.CurrentCalcluation}{CalculationId}"))
               .Returns((CalculationCurrentVersion)null);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await service.GetCalculationCurrentVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .ShouldBeEquivalentTo(new CalculationCurrentVersion
                {
                    Author = new Reference(UserId, Username),
                    Date = lastModifiedDate,
                    SourceCode = "source code",
                    Version = 1,
                    Name = "any name",
                    Id = CalculationId,
                    CalculationSpecification = new Reference("any name", "any-id"),
                    FundingPeriodName = "2018/2019",
                    FundingPeriodId = "18/19",
                    CalculationType = "Number",
                    SpecificationId = specificationId,
                    Status = "Draft",
                });

            await calculationsRepository
                .Received(1)
                .GetCalculationById(Arg.Is<string>(CalculationId));

            await cacheProvider
                .Received(1)
                .GetAsync<CalculationCurrentVersion>(Arg.Is($"{CacheKeys.CurrentCalcluation}{CalculationId}"));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenNoCalculationIdWasProvided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No calculation Id was provided to GetCalculationHistory"));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationIdButNoModelSupplied_ReturnsBadRequest()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is($"Null or empty source code was provided for calculation id {CalculationId}"));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenModelDoesNotContainSourceCiode_ReturnsBadRequest()
        {
            //Arrange
            SaveSourceCodeVersion model = new SaveSourceCodeVersion();
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is($"Null or empty source code was provided for calculation id {CalculationId}"));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationExistsWithNoHistory_CreatesNewVersion()
        {
            //Arrange
            Calculation calculation = CreateCalculation();

            SaveSourceCodeVersion model = new SaveSourceCodeVersion
            {
                SourceCode = "source code"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository,
                searchRepository: searchRepository,
                specificationRepository: specificationRepository);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"History for {CalculationId} was null or empty and needed recreating."));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenModelButCalculationDoesNotExist_ReturnsNotFound()
        {
            //Arrange
            SaveSourceCodeVersion model = new SaveSourceCodeVersion
            {
                SourceCode = "source code"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns((Calculation)null);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Error(Arg.Is($"A calculation was not found for calculation id {CalculationId}"));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationExistsWithNoCurrent_CreatesNewVersion()
        {
            //Arrange
            Calculation calculation = CreateCalculation();
            calculation.Current = null;

            SaveSourceCodeVersion model = new SaveSourceCodeVersion
            {
                SourceCode = "source code"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository,
                searchRepository: searchRepository,
                specificationRepository: specificationRepository);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Current for {CalculationId} was null and needed recreating."));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationExistsWithNoBuildId_CreatesNewBuildProject()
        {
            //Arrange
            Calculation calculation = CreateCalculation();

            SaveSourceCodeVersion model = new SaveSourceCodeVersion
            {
                SourceCode = "source code"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository,
                searchRepository: searchRepository,
                specificationRepository: specificationRepository);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Build project for specification {calculation.SpecificationId} could not be found, creating a new one"));

            await
                buildProjectsRepository
                    .Received(1)
                    .CreateBuildProject(Arg.Any<BuildProject>());
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationExistsWithButBuildProjectDoesNotExist_CreatesNewBuildProject()
        {
            // Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            Calculation calculation = CreateCalculation();
            calculation.BuildProjectId = buildProjectId;

            SaveSourceCodeVersion model = new SaveSourceCodeVersion
            {
                SourceCode = "source code"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository,
                searchRepository: searchRepository,
                specificationRepository: specificationRepository);

            // Act
            IActionResult result = await service.SaveCalculationVersion(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Build project for specification {calculation.SpecificationId} could not be found, creating a new one"));

            await
                buildProjectsRepository
                    .Received(1)
                    .CreateBuildProject(Arg.Any<BuildProject>());
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationExistsWithBuildIdButNoCalculations_EnsuresCalculationsCreatedUpdatesBuildProject()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId
            };

            Calculation calculation = CreateCalculation();
            calculation.BuildProjectId = buildProjectId;

            SaveSourceCodeVersion model = new SaveSourceCodeVersion
            {
                SourceCode = "source code"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectById(Arg.Is(buildProjectId))
                .Returns(buildProject);

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository,
                searchRepository: searchRepository,
                specificationRepository: specificationRepository);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Build project for specification {calculation.SpecificationId} could not be found, creating a new one"));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationExistsWithBuildIdButNoCalculations_EnsuresCalculationSpecificationDescriptionSetWithSingleCalculation()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId
            };

            string specificationId = "789";

            List<Models.Specs.Calculation> specCalculations = new List<Models.Specs.Calculation>();

            Models.Specs.Calculation specCalculation = new Models.Specs.Calculation()
            {
                Id = "1234",
                Name = "Calculation Name",
                Description = "Calculation Description"
            };

            specCalculations.Add(specCalculation);

            List<Calculation> calcCalculations = new List<Calculation>();

            Calculation calculation = CreateCalculation();
            calculation.BuildProjectId = buildProjectId;
            calculation.SpecificationId = specificationId;
            calculation.CalculationSpecification.Id = specCalculation.Id;
            calculation.CalculationSpecification.Name = specCalculation.Name;

            calcCalculations.Add(calculation);

            SaveSourceCodeVersion model = new SaveSourceCodeVersion
            {
                SourceCode = "source code"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(specificationId)
                .Returns(calcCalculations.AsEnumerable());

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectById(Arg.Is(buildProjectId))
                .Returns(buildProject);

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetCalculationSpecificationsForSpecification(specificationId)
                .Returns(specCalculations.AsEnumerable());

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            ISourceFileGenerator sourceFileGenerator = Substitute.For<ISourceFileGenerator>();

            ISourceFileGeneratorProvider sourceFileGeneratorProvider = Substitute.For<ISourceFileGeneratorProvider>();
            sourceFileGeneratorProvider.CreateSourceFileGenerator(TargetLanguage.VisualBasic)
                .Returns(sourceFileGenerator);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                specificationRepository: specificationRepository,
                buildProjectsRepository: buildProjectsRepository,
                searchRepository: searchRepository,
                sourceFileGeneratorProvider: sourceFileGeneratorProvider);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Build project for specification {calculation.SpecificationId} could not be found, creating a new one"));

            await calculationsRepository
                .Received(1)
                .UpdateCalculation(Arg.Is<Calculation>(c => c.Description == specCalculation.Description));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationExistsWithBuildIdButNoCalculations_EnsuresCalculationSpecificationDescriptionSetWithMultipleCalculations()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId
            };

            string specificationId = "789";

            List<Models.Specs.Calculation> specCalculations = new List<Models.Specs.Calculation>();

            Models.Specs.Calculation specCalculation1 = new Models.Specs.Calculation()
            {
                Id = "121",
                Name = "Calculation One",
                Description = "Calculation Description One"
            };

            specCalculations.Add(specCalculation1);

            Models.Specs.Calculation specCalculation2 = new Models.Specs.Calculation()
            {
                Id = "122",
                Name = "Calculation Two",
                Description = "Calculation Description Two"
            };

            specCalculations.Add(specCalculation2);

            List<Calculation> calcCalculations = new List<Calculation>();

            Calculation calculation = CreateCalculation();
            calculation.BuildProjectId = buildProjectId;
            calculation.SpecificationId = specificationId;
            calculation.CalculationSpecification.Id = specCalculation1.Id;
            calculation.CalculationSpecification.Name = specCalculation1.Name;

            calcCalculations.Add(calculation);

            Calculation calculation2 = CreateCalculation();
            calculation2.Id = "12555";
            calculation2.BuildProjectId = buildProjectId;
            calculation2.SpecificationId = specificationId;
            calculation2.CalculationSpecification.Id = specCalculation2.Id;
            calculation2.CalculationSpecification.Name = specCalculation2.Name;

            calcCalculations.Add(calculation2);

            SaveSourceCodeVersion model = new SaveSourceCodeVersion
            {
                SourceCode = "source code"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            calculationsRepository
                .GetCalculationsBySpecificationId(specificationId)
                .Returns(calcCalculations.AsEnumerable());

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectById(Arg.Is(buildProjectId))
                .Returns(buildProject);

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetCalculationSpecificationsForSpecification(specificationId)
                .Returns(specCalculations.AsEnumerable());

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            ISourceFileGenerator sourceFileGenerator = Substitute.For<ISourceFileGenerator>();

            ISourceFileGeneratorProvider sourceFileGeneratorProvider = Substitute.For<ISourceFileGeneratorProvider>();
            sourceFileGeneratorProvider.CreateSourceFileGenerator(TargetLanguage.VisualBasic)
                .Returns(sourceFileGenerator);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                specificationRepository: specificationRepository,
                buildProjectsRepository: buildProjectsRepository,
                searchRepository: searchRepository,
                sourceFileGeneratorProvider: sourceFileGeneratorProvider);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Build project for specification {calculation.SpecificationId} could not be found, creating a new one"));

            sourceFileGenerator
                .Received()
                .GenerateCode(Arg.Any<BuildProject>(), Arg.Is<IEnumerable<Calculation>>(b =>
                b.First().Description == specCalculation1.Description &&
                b.First().CalculationSpecification.Id == specCalculation1.Id &&
                b.Skip(1).First().Description == specCalculation2.Description &&
                b.Skip(1).First().CalculationSpecification.Id == specCalculation2.Id));

            await calculationsRepository
                .Received(1)
                .UpdateCalculation(Arg.Is<Calculation>(c => c.Description == specCalculation1.Description));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationExistsWithBuildIdButCalculationCouldNotBeFound_AddsCalculationUpdatesBuildProject()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId,
            };

            Calculation calculation = CreateCalculation();
            calculation.BuildProjectId = buildProjectId;

            SaveSourceCodeVersion model = new SaveSourceCodeVersion
            {
                SourceCode = "source code"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectById(Arg.Is(buildProjectId))
                .Returns(buildProject);

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
               buildProjectsRepository: buildProjectsRepository,
               searchRepository: searchRepository,
               specificationRepository: specificationRepository);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Build project for specification {calculation.SpecificationId} could not be found, creating a new one"));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationExistsWithBuildIdButButNotInSearch_CreatesSearchDocument()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId,
            };

            Calculation calculation = CreateCalculation();

            SaveSourceCodeVersion model = new SaveSourceCodeVersion
            {
                SourceCode = "source code"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectById(Arg.Is(buildProjectId))
                .Returns(buildProject);

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns((CalculationIndex)null);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationService service = CreateCalculationService(logger: logger,
                calculationsRepository: calculationsRepository,
               buildProjectsRepository: buildProjectsRepository,
               searchRepository: searchRepository,
               specificationRepository: specificationRepository);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(1)
                    .Index(Arg.Any<IList<CalculationIndex>>());
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationIsCurrentlyPublished_SetsPublishStateToUpdated()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId,
            };

            Calculation calculation = CreateCalculation();
            calculation.Current.PublishStatus = PublishStatus.Published;

            SaveSourceCodeVersion model = new SaveSourceCodeVersion
            {
                SourceCode = "source code"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectById(Arg.Is(buildProjectId))
                .Returns(buildProject);

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
               buildProjectsRepository: buildProjectsRepository,
               searchRepository: searchRepository,
               specificationRepository: specificationRepository);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            calculation
                .Current
                .PublishStatus
                .Should()
                .Be(PublishStatus.Updated);

            await
                searchRepository
                .Received(1)
                .Index(Arg.Is<IList<CalculationIndex>>(m => m.First().Status == "Updated"));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationIsCurrentlyUpdated_SetsPublishStateToUpdated()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            string specificationId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId,
            };

            Calculation calculation = CreateCalculation();
            calculation.BuildProjectId = buildProjectId;
            calculation.SpecificationId = specificationId;

            calculation.Current.PublishStatus = PublishStatus.Updated;

            SaveSourceCodeVersion model = new SaveSourceCodeVersion
            {
                SourceCode = "source code"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectById(Arg.Is(buildProjectId))
                .Returns(buildProject);

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            IMessengerService messengerService = CreateMessengerService();

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
               buildProjectsRepository: buildProjectsRepository,
               searchRepository: searchRepository,
               messengerService: messengerService,
               specificationRepository: specificationRepository);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            calculation
                .Current
                .PublishStatus
                .Should()
                .Be(PublishStatus.Updated);

            await
                searchRepository
                    .Received(1)
                    .Index(Arg.Is<IList<CalculationIndex>>(m => m.First().Status == "Updated"));

            await
                messengerService
                    .Received(1)
                    .SendToQueue(Arg.Is("calc-events-instruct-generate-allocations"),
                        Arg.Any<BuildProject>(),
                        Arg.Any<IDictionary<string, string>>());
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationUpdateFails_ThenExceptionIsThrown()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            string specificationId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId,
            };

            Calculation calculation = CreateCalculation();
            calculation.BuildProjectId = buildProjectId;
            calculation.SpecificationId = specificationId;

            calculation.Current.PublishStatus = PublishStatus.Updated;

            SaveSourceCodeVersion model = new SaveSourceCodeVersion
            {
                SourceCode = "source code"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.InternalServerError);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectById(Arg.Is(buildProjectId))
                .Returns(buildProject);

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            IMessengerService messengerService = CreateMessengerService();

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
               buildProjectsRepository: buildProjectsRepository,
               searchRepository: searchRepository,
               messengerService: messengerService,
               specificationRepository: specificationRepository);

            //Act
            Func<Task<IActionResult>> resultFunc = async () => await service.SaveCalculationVersion(request);

            //Assert
            resultFunc
                .ShouldThrow<InvalidOperationException>()
                .Which
                .Message
                .Should()
                .Be("Update calculation returned status code 'InternalServerError' instead of OK");
        }

        [TestMethod]
        async public Task PublishCalculationVersion_GivenNoCalculationIdWasprovided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            IActionResult result = await service.PublishCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No calculation Id was provided to PublishCalculationVersion"));
        }

        [TestMethod]
        async public Task PublishCalculationVersion_GivenCalculationIdWasProvidedButCalculationCouldNotBeFound_ReturnsNotFound()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns((Calculation)null);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository);

            //Act
            IActionResult result = await service.PublishCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"A calculation was not found for calculation id {CalculationId}"));
        }

        [TestMethod]
        async public Task PublishCalculationVersion_GivenCalculationIdWasProvidedButCalculationCurrentCouldNotBeFound_ReturnsNotFound()
        {
            //Arrange
            Calculation calculation = new Calculation();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository);

            //Act
            IActionResult result = await service.PublishCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"A current calculation was not found for calculation id {CalculationId}"));
        }

        [TestMethod]
        async public Task PublishCalculationVersion_GivenCalculationIdWasProvidedButAlreadyPublished_ReturnsOKResulkt()
        {
            //Arrange
            Calculation calculation = new Calculation
            {
                Current = new CalculationVersion
                {
                    PublishStatus = PublishStatus.Published
                }
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository);

            //Act
            IActionResult result = await service.PublishCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        async public Task PublishCalculationVersion_GivenCalculationIsDraftButNoBuildProjectIdFound_PublishesAndCreratesBuildProjectIdReturnsOKResult()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            Calculation calculation = CreateCalculation();
            calculation.BuildProjectId = buildProjectId;

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository, specificationRepository: specificationRepository);

            //Act
            IActionResult result = await service.PublishCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Build project for specification {calculation.SpecificationId} could not be found, creating a new one"));
        }

        [TestMethod]
        public void UpdateCalulationsForSpecification_GivenInvalidModel_LogsDoesNotSave()
        {
            //Arrange
            dynamic anyObject = new { something = 1 };

            string json = JsonConvert.SerializeObject(anyObject);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            CalculationService service = CreateCalculationService();

            //Act
            Func<Task> test = async () => await service.UpdateCalculationsForSpecification(message);

            //Assert
            test
              .ShouldThrowExactly<InvalidModelException>();
        }

        [TestMethod]
        public async Task UpdateCalulationsForSpecification_GivenModelHasNoChanges_LogsAndReturns()
        {
            //Arrange
            Models.Specs.SpecificationVersionComparisonModel specificationVersionComparison = new Models.Specs.SpecificationVersionComparisonModel()
            {
                Current = new Models.Specs.SpecificationVersion { FundingPeriod = new Reference { Id = "fp1" } },
                Previous = new Models.Specs.SpecificationVersion { FundingPeriod = new Reference { Id = "fp1" } }
            };

            string json = JsonConvert.SerializeObject(specificationVersionComparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            await service.UpdateCalculationsForSpecification(message);

            //Assert
            logger
                .Received(1)
                .Information(Arg.Is("No changes detected"));
        }

        [TestMethod]
        public async Task UpdateCalulationsForSpecification_GivenModelHasChangedFundingPeriodsButCalcculationsCouldNotBeFound_LogsAndReturns()
        {
            //Arrange
            const string specificationId = "spec-id";

            Models.Specs.SpecificationVersionComparisonModel specificationVersionComparison = new Models.Specs.SpecificationVersionComparisonModel()
            {
                Id = specificationId,
                Current = new Models.Specs.SpecificationVersion { FundingPeriod = new Reference { Id = "fp2" } },
                Previous = new Models.Specs.SpecificationVersion { FundingPeriod = new Reference { Id = "fp1" } }
            };

            string json = JsonConvert.SerializeObject(specificationVersionComparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns((IEnumerable<Calculation>)null);

            CalculationService service = CreateCalculationService(calculationsRepository, logger);

            //Act
            await service.UpdateCalculationsForSpecification(message);

            //Assert
            logger
                .Received(1)
                .Information(Arg.Is($"No calculations found for specification id: {specificationId}"));
        }

        [TestMethod]
        public async Task UpdateCalulationsForSpecification_GivenModelHasChangedFundingPeriodsButBuildProjectNotFound_EnsuresCreatesBuildProject()
        {
            //Arrange
            const string specificationId = "spec-id";

            Models.Specs.SpecificationVersionComparisonModel specificationVersionComparison = new Models.Specs.SpecificationVersionComparisonModel()
            {
                Id = specificationId,
                Current = new Models.Specs.SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp2" },
                    Name = "any-name"
                },
                Previous = new Models.Specs.SpecificationVersion { FundingPeriod = new Reference { Id = "fp1" } }
            };

            string json = JsonConvert.SerializeObject(specificationVersionComparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            IEnumerable<Calculation> calcs = new[]
            {
                new Calculation
                {
                    SpecificationId =  "spec-id",
                    Name = "any name",
                    Id = "any-id",
                    CalculationSpecification = new Reference("any name", "any-id"),
                    FundingPeriod = new Reference("18/19", "2018/2019"),
                    CalculationType = CalculationType.Number,
                    FundingStream = new Reference("fs1","fs1-111"),
                    Current = new CalculationVersion
                    {
                        Author = new Reference(UserId, Username),
                        Date = DateTime.UtcNow,
                        PublishStatus = PublishStatus.Draft,
                        SourceCode = "source code",
                        Version = 1
                    },
                    Policies = new List<Reference>()
                }
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calcs);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();

            CalculationService service = CreateCalculationService(calculationsRepository, logger, buildProjectsRepository: buildProjectsRepository);

            //Act
            await service.UpdateCalculationsForSpecification(message);

            //Assert
            await
                buildProjectsRepository
                    .Received(1)
                    .CreateBuildProject(Arg.Any<BuildProject>());

            logger
                .Received(1)
                .Warning(Arg.Is($"A build project could not be found for specification id: {specificationId}"));
        }

        [TestMethod]
        public async Task UpdateCalculationsForSpecification_GivenModelHasChangedFundingPeriodsButBuildProjectNotFound_AssignsCalculationsToBuildProjectAndSaves()
        {
            // Arrange
            const string specificationId = "spec-id";

            Models.Specs.SpecificationVersionComparisonModel specificationVersionComparison = new Models.Specs.SpecificationVersionComparisonModel()
            {
                Id = specificationId,
                Current = new Models.Specs.SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp2" },
                    Name = "any-name"
                },
                Previous = new Models.Specs.SpecificationVersion { FundingPeriod = new Reference { Id = "fp1" } }
            };

            string json = JsonConvert.SerializeObject(specificationVersionComparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            IEnumerable<Calculation> calcs = new[]
            {
                new Calculation
                {
                    SpecificationId =  "spec-id",
                    Name = "any name",
                    Id = "any-id",
                    CalculationSpecification = new Reference("any name", "any-id"),
                    FundingPeriod = new Reference("18/19", "2018/2019"),
                    CalculationType = CalculationType.Number,
                    FundingStream = new Reference("fs1","fs1-111"),
                    Current = new CalculationVersion
                    {
                        Author = new Reference(UserId, Username),
                        Date = DateTime.UtcNow,
                        PublishStatus = PublishStatus.Draft,
                        SourceCode = "source code",
                        Version = 1
                    },
                    Policies = new List<Reference>()
                }
            };

            BuildProject buildProject = null;

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calcs);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(specificationId))
                .Returns(buildProject);

            IMessengerService messengerService = CreateMessengerService();

            CalculationService service = CreateCalculationService(calculationsRepository, logger, buildProjectsRepository: buildProjectsRepository, messengerService: messengerService);

            // Act
            await service.UpdateCalculationsForSpecification(message);

            // Assert
            calcs
                .First()
                .FundingPeriod.Id
                .Should()
                .Be("fp2");

            await buildProjectsRepository
               .Received(1)
               .CreateBuildProject(Arg.Any<BuildProject>());

            await
                messengerService
                    .Received(1)
                    .SendToQueue(Arg.Is(ServiceBusConstants.QueueNames.CalculationJobInitialiser), Arg.Any<BuildProject>(), Arg.Any<IDictionary<string, string>>());
        }

        [TestMethod]
        public async Task UpdateCalculationsForSpecification_GivenModelHasChangedPolicyName_SavesChanges()
        {
            // Arrange
            const string specificationId = "spec-id";

            Models.Specs.SpecificationVersionComparisonModel specificationVersionComparison = new Models.Specs.SpecificationVersionComparisonModel()
            {
                Id = specificationId,
                Current = new Models.Specs.SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" },
                    Name = "any-name",
                    Policies = new[] { new Models.Specs.Policy { Id = "pol-id", Name = "policy2" } }
                },
                Previous = new Models.Specs.SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" },
                    Policies = new[] { new Models.Specs.Policy { Id = "pol-id", Name = "policy1" } }
                }
            };

            string json = JsonConvert.SerializeObject(specificationVersionComparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            IEnumerable<Calculation> calcs = new[]
            {
                new Calculation
                {
                    SpecificationId =  "spec-id",
                    Name = "any name",
                    Id = "any-id",
                    CalculationSpecification = new Reference("any name", "any-id"),
                    FundingPeriod = new Reference("18/19", "2018/2019"),
                    CalculationType = CalculationType.Number,
                    FundingStream = new Reference("fp1","fs1-111"),
                    Current = new CalculationVersion
                    {
                        Author = new Reference(UserId, Username),
                        Date = DateTime.UtcNow,
                        PublishStatus = PublishStatus.Draft,
                        SourceCode = "source code",
                        Version = 1
                    },
                    Policies = new List<Reference>{ new Reference { Id = "pol-id", Name = "policy1"} }
                }
            };

            BuildProject buildProject = null;

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calcs);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(specificationId))
                .Returns(buildProject);

            IMessengerService messengerService = CreateMessengerService();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            CalculationService service = CreateCalculationService(calculationsRepository, logger, buildProjectsRepository: buildProjectsRepository, searchRepository: searchRepository);

            // Act
            await service.UpdateCalculationsForSpecification(message);

            // Assert
            calcs
                .First()
                .Policies
                .First()
                .Name
                .Should()
                .Be("policy2");

            await
                searchRepository
                    .Received(1)
                    .Index(Arg.Is<IEnumerable<CalculationIndex>>(m => m.First().PolicySpecificationNames.Contains("policy2")));
        }

        [TestMethod]
        public async Task UpdateCalculationsForSpecification_GivenModelHasChangedFundingStreams_SetsTheAllocationLineAndFundingStreamToNull()
        {
            //Arrange
            const string specificationId = "spec-id";

            Models.Specs.SpecificationVersionComparisonModel specificationVersionComparison = new Models.Specs.SpecificationVersionComparisonModel()
            {
                Id = specificationId,
                Current = new Models.Specs.SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" },
                    Name = "any-name",
                    FundingStreams = new List<Reference> { new Reference { Id = "fs2" } }
                },
                Previous = new Models.Specs.SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" },
                    FundingStreams = new List<Reference> { new Reference { Id = "fs1" } }
                }
            };

            string json = JsonConvert.SerializeObject(specificationVersionComparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            IEnumerable<Calculation> calcs = new[]
            {
                new Calculation
                {
                    SpecificationId =  "spec-id",
                    Name = "any name",
                    Id = "any-id",
                    CalculationSpecification = new Reference("any name", "any-id"),
                    FundingPeriod = new Reference("18/19", "2018/2019"),
                    CalculationType = CalculationType.Number,
                    FundingStream = new Reference("fs1","fs1-111"),
                    Current = new CalculationVersion
                    {
                        Author = new Reference(UserId, Username),
                        Date = DateTime.UtcNow,
                        PublishStatus = PublishStatus.Draft,
                        SourceCode = "source code",
                        Version = 1
                    },
                    Policies = new List<Reference>()
                }
            };

            BuildProject buildProject = new BuildProject();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calcs);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(specificationId))
                .Returns(buildProject);

            IMessengerService messengerService = CreateMessengerService();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            CalculationService service = CreateCalculationService(calculationsRepository, logger, buildProjectsRepository: buildProjectsRepository, messengerService: messengerService, searchRepository: searchRepository);

            //Act
            await service.UpdateCalculationsForSpecification(message);

            //Assert
            calcs
                .First()
                .FundingStream
                .Should()
                .BeNull();

            calcs
               .First()
               .AllocationLine
               .Should()
               .BeNull();

            await searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<CalculationIndex>>(c =>
                    c.First().Id == calcs.First().Id &&
                    c.First().FundingStreamId == "" &&
                    c.First().FundingStreamName == "No funding stream set"));
            await
                messengerService
                    .Received(1)
                    .SendToQueue(Arg.Is(ServiceBusConstants.QueueNames.CalculationJobInitialiser), Arg.Is(buildProject), Arg.Any<IDictionary<string, string>>());
        }

        [TestMethod]
        public async Task UpdateCalculationCodeOnCalculationSpecificationChange_WhenCalculationsFoundReferencingCalculationToBeUpdated_ThenSourceCodeUpdated()
        {
            // Arrange
            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            ISpecificationRepository specificationRepository = CreateSpecificationRepository();

            CalculationService service = CreateCalculationService(calculationsRepository: calculationsRepository, specificationRepository: specificationRepository);

            const string specificationId = "specId";
            const string calculationId = "updatedCalc";

            Models.Specs.CalculationVersionComparisonModel comparison = new Models.Specs.CalculationVersionComparisonModel()
            {
                CalculationId = calculationId,
                Current = new Models.Specs.Calculation()
                {
                    Id = "calcSpec1",
                    Name = "Calculation To Update",
                    CalculationType = CalculationType.Funding,
                },
                Previous = new Models.Specs.Calculation()
                {
                    Id = "calcSpec1",
                    Name = "Original Name",
                    CalculationType = CalculationType.Funding,
                },
                SpecificationId = specificationId,
            };

            Reference user = new Reference("userId", "User Name");

            List<Calculation> calculations = new List<Calculation>();
            Calculation calc1 = new Calculation()
            {
                Id = calculationId,
                Name = "Calculation to Update",
                SpecificationId = specificationId,
                FundingPeriod = new Reference("fp1", "Funding Period"),
                History = new List<CalculationVersion>(),
                CalculationSpecification = new Reference("calcSpec1", "Calculation to Update"),
                CalculationType = CalculationType.Funding,
                Description = "Calculation Description",
                BuildProjectId = "bpC1",
                Policies = new List<Reference>(),
            };
            calc1.Save(new CalculationVersion()
            {
                SourceCode = "Return 10",
                DecimalPlaces = 6,
            });

            calculations.Add(calc1);

            Calculation calc2 = new Calculation()
            {
                Id = "referenceCalc",
                Name = "Calling Calculation To Update",
                SpecificationId = specificationId,
                FundingPeriod = new Reference("fp1", "Funding Period"),
                History = new List<CalculationVersion>(),
                CalculationSpecification = new Reference("calcSpec1", "Calculation to Update"),
                CalculationType = CalculationType.Funding,
                Description = "Calculation Description",
                BuildProjectId = "bpC1",
                Policies = new List<Reference>(),
            };
            calc2.Save(new CalculationVersion()
            {
                SourceCode = "Return OriginalName()",
                DecimalPlaces = 6,
            });


            calculations.Add(calc2);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calculations);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            calculationsRepository
                .GetCalculationById(Arg.Is(calculations[0].Id))
                .Returns(calculations[0]);

            Models.Specs.SpecificationSummary specification = new Models.Specs.SpecificationSummary()
            {
                Id = specificationId,
                Name = "Specification Name",
            };

            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specification);

            // Act
            IEnumerable<Calculation> updatedCalculations = await service.UpdateCalculationCodeOnCalculationSpecificationChange(comparison, user);

            // Assert
            updatedCalculations
                .Should()
                .HaveCount(1);

            updatedCalculations
                .First()
                .Current.SourceCode
                .Should()
                .Be("Return CalculationToUpdate()");

            updatedCalculations
                .First()
                .Current.Version
                .Should()
                .Be(2);

            updatedCalculations
                .First()
                .Id
                .Should()
                .Be("referenceCalc");
        }

        [TestMethod]
        public async Task UpdateCalculationCodeOnCalculationSpecificationChange_WhenCalculationsFoundReferencingCalculationToBeUpdatedHasDifferentNameCasing_ThenSourceCodeUpdated()
        {
            // Arrange
            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            ISpecificationRepository specificationRepository = CreateSpecificationRepository();

            CalculationService service = CreateCalculationService(calculationsRepository: calculationsRepository, specificationRepository: specificationRepository);

            const string specificationId = "specId";
            const string calculationId = "updatedCalc";

            Models.Specs.CalculationVersionComparisonModel comparison = new Models.Specs.CalculationVersionComparisonModel()
            {
                CalculationId = calculationId,
                Current = new Models.Specs.Calculation()
                {
                    Id = "calcSpec1",
                    Name = "Calculation to update",
                    CalculationType = CalculationType.Funding,
                },
                Previous = new Models.Specs.Calculation()
                {
                    Id = "calcSpec1",
                    Name = "Original Name",
                    CalculationType = CalculationType.Funding,
                },
                SpecificationId = specificationId,
            };

            Reference user = new Reference("userId", "User Name");

            List<Calculation> calculations = new List<Calculation>();
            Calculation calc1 = new Calculation()
            {
                Id = calculationId,
                Name = "Calculation to Update",
                SpecificationId = specificationId,
                FundingPeriod = new Reference("fp1", "Funding Period"),
                History = new List<CalculationVersion>(),
                CalculationSpecification = new Reference("calcSpec1", "Calculation to Update"),
                CalculationType = CalculationType.Funding,
                Description = "Calculation Description",
                BuildProjectId = "bpC1",
                Policies = new List<Reference>(),
            };
            calc1.Save(new CalculationVersion()
            {
                SourceCode = "Return 10",
                DecimalPlaces = 6,
            });

            calculations.Add(calc1);

            Calculation calc2 = new Calculation()
            {
                Id = "referenceCalc",
                Name = "Calling Calculation To Update",
                SpecificationId = specificationId,
                FundingPeriod = new Reference("fp1", "Funding Period"),
                History = new List<CalculationVersion>(),
                CalculationSpecification = new Reference("calcSpec1", "Calculation to Update"),
                CalculationType = CalculationType.Funding,
                Description = "Calculation Description",
                BuildProjectId = "bpC1",
                Policies = new List<Reference>(),
            };
            calc2.Save(new CalculationVersion()
            {
                SourceCode = "Return OriginalName()",
                DecimalPlaces = 6,
            });

            calculations.Add(calc2);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calculations);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            calculationsRepository
                .GetCalculationById(Arg.Is(calculations[0].Id))
                .Returns(calculations[0]);

            Models.Specs.SpecificationSummary specification = new Models.Specs.SpecificationSummary()
            {
                Id = specificationId,
                Name = "Specification Name",
            };

            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specification);

            // Act
            IEnumerable<Calculation> updatedCalculations = await service.UpdateCalculationCodeOnCalculationSpecificationChange(comparison, user);

            // Assert
            updatedCalculations
                .Should()
                .HaveCount(1);

            updatedCalculations
                .First()
                .Current.SourceCode
                .Should()
                .Be("Return CalculationToUpdate()");

            updatedCalculations
                .First()
                .Current.Version
                .Should()
                .Be(2);

            updatedCalculations
                .First()
                .Id
                .Should()
                .Be("referenceCalc");
        }

        [TestMethod]
        public async Task UpdateCalculationCodeOnCalculationSpecificationChange_WhenNoCalculationsFoundReferencingCalculationToBeUpdated_ThenNoCalculationsUpdated()
        {
            // Arrange
            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            ISpecificationRepository specificationRepository = CreateSpecificationRepository();

            CalculationService service = CreateCalculationService(calculationsRepository: calculationsRepository, specificationRepository: specificationRepository);

            const string specificationId = "specId";
            const string calculationId = "updatedCalc";

            Models.Specs.CalculationVersionComparisonModel comparison = new Models.Specs.CalculationVersionComparisonModel()
            {
                CalculationId = calculationId,
                Current = new Models.Specs.Calculation()
                {
                    Id = "calcSpec1",
                    Name = "Calculation to update",
                    CalculationType = CalculationType.Funding,
                },
                Previous = new Models.Specs.Calculation()
                {
                    Id = "calcSpec1",
                    Name = "Original Name",
                    CalculationType = CalculationType.Funding,
                },
                SpecificationId = specificationId,
            };

            Reference user = new Reference("userId", "User Name");

            List<Calculation> calculations = new List<Calculation>();
            Calculation calc1 = new Calculation()
            {
                Id = calculationId,
                Name = "Calculation to Update",
                SpecificationId = specificationId,
                FundingPeriod = new Reference("fp1", "Funding Period"),
                History = new List<CalculationVersion>(),
                CalculationSpecification = new Reference("calcSpec1", "Calculation to Update"),
                CalculationType = CalculationType.Funding,
                Description = "Calculation Description",
                BuildProjectId = "bpC1",
                Policies = new List<Reference>(),
            };
            calc1.Save(new CalculationVersion()
            {
                SourceCode = "Return 10",
                DecimalPlaces = 6,
            });

            calculations.Add(calc1);

            Calculation calc2 = new Calculation()
            {
                Id = "referenceCalc",
                Name = "Calling Calculation To Update",
                SpecificationId = specificationId,
                FundingPeriod = new Reference("fp1", "Funding Period"),
                History = new List<CalculationVersion>(),
                CalculationSpecification = new Reference("calcSpec1", "Calculation to Update"),
                CalculationType = CalculationType.Funding,
                Description = "Calculation Description",
                BuildProjectId = "bpC1",
                Policies = new List<Reference>(),
            };
            calc2.Save(new CalculationVersion()
            {
                SourceCode = "Return 50",
                DecimalPlaces = 6,
            });


            calculations.Add(calc2);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calculations);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            calculationsRepository
                .GetCalculationById(Arg.Is(calculations[0].Id))
                .Returns(calculations[0]);

            Models.Specs.SpecificationSummary specification = new Models.Specs.SpecificationSummary()
            {
                Id = specificationId,
                Name = "Specification Name",
            };

            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specification);

            // Act
            IEnumerable<Calculation> updatedCalculations = await service.UpdateCalculationCodeOnCalculationSpecificationChange(comparison, user);

            // Assert
            updatedCalculations
                .Should()
                .HaveCount(0);
        }

        static CalculationService CreateCalculationService(
            ICalculationsRepository calculationsRepository = null,
            ILogger logger = null,
            ITelemetry telemetry = null,
            ISearchRepository<CalculationIndex> searchRepository = null,
            IValidator<Calculation> calcValidator = null,
            IBuildProjectsRepository buildProjectsRepository = null,
            ISourceFileGeneratorProvider sourceFileGeneratorProvider = null,
            ICompilerFactory compilerFactory = null,
            IMessengerService messengerService = null,
            ICodeMetadataGeneratorService codeMetadataGenerator = null,
            ISpecificationRepository specificationRepository = null,
            ICacheProvider cacheProvider = null)
        {
            return new CalculationService
                (calculationsRepository ?? CreateCalculationsRepository(),
                logger ?? CreateLogger(),
                telemetry ?? CreateTelemetry(),
                searchRepository ?? CreateSearchRepository(),
                calcValidator ?? CreateCalculationValidator(),
                buildProjectsRepository ?? CreateBuildProjectsRepository(),
                sourceFileGeneratorProvider ?? CreateSourceFileGeneratorProvider(),
                compilerFactory ?? CreateCompilerFactory(),
                messengerService ?? CreateMessengerService(),
                codeMetadataGenerator ?? CreateCodeMetadataGenerator(),
                specificationRepository ?? CreateSpecificationRepository(),
                cacheProvider ?? CreateCacheProvider());
        }

        static ICalculationsRepository CreateCalculationsRepository()
        {
            return Substitute.For<ICalculationsRepository>();
        }

        static IBuildProjectsRepository CreateBuildProjectsRepository()
        {
            return Substitute.For<IBuildProjectsRepository>();
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static ITelemetry CreateTelemetry()
        {
            return Substitute.For<ITelemetry>();
        }

        static ISearchRepository<CalculationIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<CalculationIndex>>();
        }

        static ISourceFileGeneratorProvider CreateSourceFileGeneratorProvider()
        {
            return Substitute.For<ISourceFileGeneratorProvider>();
        }

        static IMessengerService CreateMessengerService()
        {
            return Substitute.For<IMessengerService>();
        }

        static ISpecificationRepository CreateSpecificationRepository()
        {
            return Substitute.For<ISpecificationRepository>();
        }

        static ICodeMetadataGeneratorService CreateCodeMetadataGenerator()
        {
            return Substitute.For<ICodeMetadataGeneratorService>();
        }

        static ICompilerFactory CreateCompilerFactory()
        {
            ICompiler compiler = Substitute.For<ICompiler>();

            ICompilerFactory compilerFactory = Substitute.For<ICompilerFactory>();
            compilerFactory
                .GetCompiler(Arg.Any<IEnumerable<SourceFile>>())
                .Returns(compiler);

            return compilerFactory;
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

        static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        static Calculation CreateCalculation()
        {
            return new Calculation
            {
                Id = CalculationId,
                Name = "Test Calc Name",
                CalculationSpecification = new Reference
                {
                    Id = "any-calc-id",
                    Name = "Test Calc Name",
                },
                SpecificationId = "any-spec-id",
                FundingPeriod = new Reference
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
        }
    }
}
