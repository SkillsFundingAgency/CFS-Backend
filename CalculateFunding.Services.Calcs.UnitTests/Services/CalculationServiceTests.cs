using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
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
                .Error("A null or invalid calculation was provided to CalculateFunding.Services.Calcs.CreateCalculation");

            await
                repository
                    .DidNotReceive()
                    .CreateDraftCalculation(Arg.Any<Calculation>());
        }

        [TestMethod]
        public async Task CreateCalculation_GivenInvalidCalculation_LogsDoesNotSave()
        {
            //Arrange
            Message message = new Message();

            dynamic anyObject = new { something = 1 };

            string json = JsonConvert.SerializeObject(anyObject);

            message.Body = Encoding.UTF8.GetBytes(json);

            //message.UserProperties.Add("user-id", UserId);
            //message.UserProperties.Add("user-name", Username);

            ICalculationsRepository repository = CreateCalculationsRepository();

            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(repository, logger);

            //Act
            await service.CreateCalculation(message);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid calculation was provided to CalculateFunding.Services.Calcs.CreateCalculation");

            await
               repository
                   .DidNotReceive()
                   .CreateDraftCalculation(Arg.Any<Calculation>());
        }

        [TestMethod]
        public async Task CreateCalculation_GivenValidCalculation_ButFailedToSave()
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

            CalculationService service = CreateCalculationService(repository, logger);

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
        }

        [TestMethod]
        [Ignore("Just while i test the search stuff")]
        public async Task CreateCalculation_GivenValidCalculation_AndSavesLogs()
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
                .Returns(HttpStatusCode.Created);

            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(repository, logger);

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
        }

        static CalculationService CreateCalculationService(ICalculationsRepository calculationsRepository = null, 
            ILogger logger = null, ISearchRepository<CalculationIndex> serachRepository = null)
        {
            return new CalculationService(calculationsRepository ?? CreateCalculationsRepository(), 
                logger ?? CreateLogger(), serachRepository ?? CreateSearchRepository());
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
    }
}
