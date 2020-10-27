using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Messages;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;


namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    public partial class SpecificationsServiceTests
    {
        [TestMethod]
        public void AssignDataDefinitionRelationship_GivenMessageWithNullRealtionshipObject_ThrowsArgumentNullException()
        {
            //Arrange
            Message message = new Message(new byte[0]);

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            Func<Task> test = async () => await service.Process(message);

            //Assert
            test
                .Should().ThrowExactly<ArgumentNullException>();

            logger
                .Received()
                .Error("A null relationship message was provided to AssignDataDefinitionRelationship");
        }

        [TestMethod]
        public void AssignDataDefinitionRelationship_GivenMessageWithObjectButDoesntValidate_ThrowsInvalidModelException()
        {
            //Arrange
            dynamic anyObject = new { something = 1 };

            string json = JsonConvert.SerializeObject(anyObject);


            Message message = new Message(Encoding.UTF8.GetBytes(json));


            ValidationResult validationResult = new ValidationResult(new[]{
                    new ValidationFailure("prop1", "any error")
                });

            IValidator<AssignDefinitionRelationshipMessage> validator = CreateAssignDefinitionRelationshipMessageValidator(validationResult);

            SpecificationsService service = CreateService(assignDefinitionRelationshipMessageValidator: validator);

            //Act
            Func<Task> test = async () => await service.Process(message);

            //Assert
            test
                .Should().ThrowExactly<InvalidModelException>();
        }

        [TestMethod]
        public void AssignDataDefinitionRelationship_GivenValidMessageButUnableToFindSpecification_ThrowsInvalidModelException()
        {
            //Arrange

            dynamic anyObject = new { specificationId = SpecificationId, relationshipId = RelationshipId };

            string json = JsonConvert.SerializeObject(anyObject);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns((Specification)null);

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository);

            //Act
            Func<Task> test = async () => await service.Process(message);

            //Assert
            test
                .Should().ThrowExactly<InvalidModelException>();
        }

        [TestMethod]
        public void AssignDataDefinitionRelationship_GivenFailedToUpdateSpecification_ThrowsException()
        {
            //Arrange
            dynamic anyObject = new { specificationId = SpecificationId, relationshipId = RelationshipId };

            string json = JsonConvert.SerializeObject(anyObject);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            Specification specification = new Specification()
            {
                Current = new Models.Specs.SpecificationVersion(),
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
                .UpdateSpecification(Arg.Is(specification))
                .Returns(HttpStatusCode.InternalServerError);

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, logs: logger);

            //Act
            Func<Task> test = async () => await service.Process(message);

            //Assert
            test
                .Should().ThrowExactly<Exception>();

            logger
                .Received()
                .Error($"Failed to update specification for id: {SpecificationId} with dataset definition relationship id {RelationshipId}");
        }

        [TestMethod]
        public async Task AssignDataDefinitionRelationship_GivenUpdatedCosmosAndSearch_LogsSuccess()
        {
            //Arrange
            dynamic anyObject = new { specificationId = SpecificationId, relationshipId = RelationshipId };

            string json = JsonConvert.SerializeObject(anyObject);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            Specification specification = new Specification
            {
                Id = SpecificationId,
                Name = SpecificationName,
                Current = new Models.Specs.SpecificationVersion()
                {
                    FundingStreams = new List<Reference>() { new Reference("fs-id", "fs-name") },
                    FundingPeriod = new Reference("18/19", "2018/19"),
                },
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
                .UpdateSpecification(Arg.Is(specification))
                .Returns(HttpStatusCode.OK);

            ILogger logger = CreateLogger();

            Models.Specs.SpecificationVersion newSpecVersion = specification.Current.Clone() as Models.Specs.SpecificationVersion;

            IVersionRepository<Models.Specs.SpecificationVersion> versionRepository = CreateVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<Models.Specs.SpecificationVersion>(), Arg.Any<Models.Specs.SpecificationVersion>())
                .Returns(newSpecVersion);

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, logs: logger, specificationVersionRepository: versionRepository);

            //Act
            await service.Process(message);

            //Assert
            logger
                .Received(1)
                .Information($"Successfully assigned relationship id: {RelationshipId} to specification with id: {SpecificationId}");
            
            await _specificationIndexer
                .Received(1)
                .Index(Arg.Is<Specification>(_ => ReferenceEquals(_.Current, newSpecVersion)));
        }
    }
}
