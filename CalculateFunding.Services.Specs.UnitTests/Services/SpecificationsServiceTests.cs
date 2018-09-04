using AutoMapper;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Serilog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Collections.Generic;
using System.Text;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Models;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Models.Specs.Messages;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Models.MappingProfiles;

namespace CalculateFunding.Services.Specs.Services
{
    [TestClass]
    public partial class SpecificationsServiceTests
    {
        const string FundingStreamId = "YAPGG";
        const string SpecificationId = "ffa8ccb3-eb8e-4658-8b3f-f1e4c3a8f313";
        const string PolicyId = "dda8ccb3-eb8e-4658-8b3f-f1e4c3a8f322";
        const string AllocationLineId = "02a6eeaf-e1a0-476e-9cf9-8aa5d9129345";
        const string CalculationId = "22a6eeaf-e1a0-476e-9cf9-8aa6c51293433";
        const string FundingPeriodId = "18/19";
        const string SpecificationName = "Test Spec 001";
        const string PolicyName = "Test Policy 001";
        const string CalculationName = "Test Calc 001";
        const string Username = "test-user";
        const string UserId = "33d7a71b-f570-4425-801b-250b9129f3d3";
        const string SfaCorrelationId = "c625c3f9-6ce8-4f1f-a3a3-4611f1dc3881";
        const string RelationshipId = "cca8ccb3-eb8e-4658-8b3f-f1e4c3a8f419";
        const string yamlFile = "12345.yaml";

        static SpecificationsService CreateService(
            IMapper mapper = null,
            ISpecificationsRepository specificationsRepository = null,
            ILogger logs = null,
            IValidator<PolicyCreateModel> policyCreateModelValidator = null,
            IValidator<SpecificationCreateModel> specificationCreateModelvalidator = null,
            IValidator<CalculationCreateModel> calculationCreateModelValidator = null,
            IMessengerService messengerService = null, ServiceBusSettings EventHubSettings = null,
            ISearchRepository<SpecificationIndex> searchRepository = null,
            IValidator<AssignDefinitionRelationshipMessage> assignDefinitionRelationshipMessageValidator = null,
            ICacheProvider cacheProvider = null,
            IValidator<SpecificationEditModel> specificationEditModelValidator = null,
            IValidator<PolicyEditModel> policyEditModelValidator = null,
            IValidator<CalculationEditModel> calculationEditModelValidator = null,
            IResultsRepository resultsRepository = null)
        {
            return new SpecificationsService(mapper ?? CreateMapper(),
                specificationsRepository ?? CreateSpecificationsRepository(),
                logs ?? CreateLogger(),
                policyCreateModelValidator ?? CreatePolicyValidator(),
                specificationCreateModelvalidator ?? CreateSpecificationValidator(),
                calculationCreateModelValidator ?? CreateCalculationValidator(),
                messengerService ?? CreateMessengerService(),
                searchRepository ?? CreateSearchRepository(),
                assignDefinitionRelationshipMessageValidator ?? CreateAssignDefinitionRelationshipMessageValidator(),
                cacheProvider ?? CreateCacheProvider(),
                specificationEditModelValidator ?? CreateEditSpecificationValidator(),
                policyEditModelValidator ?? CreateEditPolicyValidator(),
                calculationEditModelValidator ?? CreateEditCalculationValidator(),
                resultsRepository ?? CreateResultsRepository());
        }

        static IResultsRepository CreateResultsRepository()
        {
            return Substitute.For<IResultsRepository>();
        }

        static IMapper CreateMapper()
        {
            return Substitute.For<IMapper>();
        }

        private static IMapper CreateImplementedMapper()
        {
            MapperConfiguration mappingConfiguration = new MapperConfiguration(c => c.AddProfile<SpecificationsMappingProfile>());
            IMapper mapper = mappingConfiguration.CreateMapper();
            return mapper;
        }

        static IMessengerService CreateMessengerService()
        {
            return Substitute.For<IMessengerService>();
        }

        static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        static ISpecificationsRepository CreateSpecificationsRepository()
        {
            return Substitute.For<ISpecificationsRepository>();
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static ISearchRepository<SpecificationIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<SpecificationIndex>>();
        }

        static IValidator<PolicyCreateModel> CreatePolicyValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
                validationResult = new ValidationResult();

            IValidator<PolicyCreateModel> validator = Substitute.For<IValidator<PolicyCreateModel>>();

            validator
               .ValidateAsync(Arg.Any<PolicyCreateModel>())
               .Returns(validationResult);

            return validator;
        }

        static IValidator<PolicyEditModel> CreateEditPolicyValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
                validationResult = new ValidationResult();

            IValidator<PolicyEditModel> validator = Substitute.For<IValidator<PolicyEditModel>>();

            validator
               .ValidateAsync(Arg.Any<PolicyEditModel>())
               .Returns(validationResult);

            return validator;
        }

        static IValidator<SpecificationCreateModel> CreateSpecificationValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
                validationResult = new ValidationResult();

            IValidator<SpecificationCreateModel> validator = Substitute.For<IValidator<SpecificationCreateModel>>();

            validator
               .ValidateAsync(Arg.Any<SpecificationCreateModel>())
               .Returns(validationResult);

            return validator;
        }

        static IValidator<SpecificationEditModel> CreateEditSpecificationValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
                validationResult = new ValidationResult();

            IValidator<SpecificationEditModel> validator = Substitute.For<IValidator<SpecificationEditModel>>();

            validator
               .ValidateAsync(Arg.Any<SpecificationEditModel>())
               .Returns(validationResult);

            return validator;
        }

        static IValidator<CalculationCreateModel> CreateCalculationValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
                validationResult = new ValidationResult();

            IValidator<CalculationCreateModel> validator = Substitute.For<IValidator<CalculationCreateModel>>();

            validator
               .ValidateAsync(Arg.Any<CalculationCreateModel>())
               .Returns(validationResult);

            return validator;
        }

        static IValidator<CalculationEditModel> CreateEditCalculationValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
                validationResult = new ValidationResult();

            IValidator<CalculationEditModel> validator = Substitute.For<IValidator<CalculationEditModel>>();

            validator
               .ValidateAsync(Arg.Any<CalculationEditModel>())
               .Returns(validationResult);

            return validator;
        }

        static IValidator<AssignDefinitionRelationshipMessage> CreateAssignDefinitionRelationshipMessageValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
                validationResult = new ValidationResult();

            IValidator<AssignDefinitionRelationshipMessage> validator = Substitute.For<IValidator<AssignDefinitionRelationshipMessage>>();

            validator
               .ValidateAsync(Arg.Any<AssignDefinitionRelationshipMessage>())
               .Returns(validationResult);

            return validator;
        }

        static string CreateRawFundingStream()
        {
            var yaml = new StringBuilder();

            yaml.AppendLine(@"id: YPLRE");
            yaml.AppendLine(@"name: School Budget Share");
            yaml.AppendLine(@"allocationLines:");
            yaml.AppendLine(@"- id: YPE01");
            yaml.AppendLine(@"  name: School Budget Share");
            yaml.AppendLine(@"- id: YPE02");
            yaml.AppendLine(@"  name: Education Services Grant");
            yaml.AppendLine(@"- id: YPE03");
            yaml.AppendLine(@"  name: Insurance");
            yaml.AppendLine(@"- id: YPE04");
            yaml.AppendLine(@"  name: Teacher Threshold");
            yaml.AppendLine(@"- id: YPE05");
            yaml.AppendLine(@"  name: Mainstreamed Grants");
            yaml.AppendLine(@"- id: YPE06");
            yaml.AppendLine(@"  name: Start Up Grant Part a");
            yaml.AppendLine(@"- id: YPE07");
            yaml.AppendLine(@"  name: Start Up Grant Part b Formulaic");


            return yaml.ToString();
        }

        static string CreateRawFundingPeriods()
        {
            var yaml = new StringBuilder();

            yaml.AppendLine(@"fundingPeriods:");
            yaml.AppendLine(@"- id: AY2017181");
            yaml.AppendLine(@"  name: Academic 2017/18");
            yaml.AppendLine(@"  startDate: 09/01/2017 00:00:00");
            yaml.AppendLine(@"  endDate: 08/31/2018 00:00:00");
            yaml.AppendLine(@"- id: AY2018191");
            yaml.AppendLine(@"  name: Academic 2018/19");
            yaml.AppendLine(@"  startDate: 09/01/2018 00:00:00");
            yaml.AppendLine(@"  endDate: 08/31/2019 00:00:00");
            yaml.AppendLine(@"- id: FY2017181");
            yaml.AppendLine(@"  name: Financial 2017/18");
            yaml.AppendLine(@"  startDate: 04/01/2017 00:00:00");
            yaml.AppendLine(@"  endDate: 03/31/2018 00:00:00");
            yaml.AppendLine(@"- id: AY2018191");
            yaml.AppendLine(@"  name: Financial 2018/19");
            yaml.AppendLine(@"  startDate: 04/01/2018 00:00:00");
            yaml.AppendLine(@"  endDate: 03/31/2019 00:00:00");

            return yaml.ToString();
        }

        static Specification CreateSpecification()
        {
            return new Specification()
            {
                Id = SpecificationId,
                Name = "Spec Name",
                Current = new SpecificationVersion()
                {
                    Name = "Spec name",
                    FundingStreams = new List<Reference>()
                    {
                         new Reference("fs1", "Funding Stream 1"),
                         new Reference("fs2", "Funding Stream 2"),
                    },
                    Author = new Reference("author@dfe.gov.uk", "Author Name"),
                    DataDefinitionRelationshipIds = new List<string>()
                       {
                           "dr1",
                           "dr2"
                       },
                    Description = "Specification Description",
                    FundingPeriod = new Reference("FP1", "Funding Period"),
                    PublishStatus = Models.Versioning.PublishStatus.Draft,
                    Version = 1
                }
            };
        }

        static IEnumerable<FundingStream> CreateFundingStreams()
        {
            return new []
            {
                new FundingStream
                {
                    Id = "PSG",
                    Name = "PE and Sport Premium Grant",
                    ShortName = "PE and Sport",
                    PeriodType = new PeriodType
                    {
                        Id = "AC",
                        StartDay = 1,
                        StartMonth = 9,
                        EndDay = 31,
                        EndMonth = 8,
                        Name = "Academies Academic Year"
                    },
                    AllocationLines = new List<AllocationLine>
                    {
                        new AllocationLine
                        {
                            Id = "PSG-NMSS",
                            Name = "Non-maintained Special Schools",
                            FundingRoute = FundingRoute.Provider,
                            IsContractRequired = true,
                            ShortName = "NMSS"
                        },
                        new AllocationLine
                        {
                            Id = "PSG-ACAD",
                            Name = "Academies",
                            FundingRoute = FundingRoute.Provider,
                            IsContractRequired = false,
                            ShortName = "Acad"
                        },
                         new AllocationLine
                        {
                            Id = "PSG-LAMS",
                            Name = "Maintained Schools",
                            FundingRoute = FundingRoute.LA,
                            IsContractRequired = false,
                            ShortName = "MS"
                        }
                    }
                }
            };
        }
    }
}
