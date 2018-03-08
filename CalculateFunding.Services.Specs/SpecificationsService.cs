using CalculateFunding.Models.Specs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using AutoMapper;
using CalculateFunding.Services.Specs.Interfaces;
using CalculateFunding.Models;
using System.Linq;
using System.Net;
using FluentValidation;
using CalculateFunding.Services.Core.Extensions;
using Serilog;
using CalculateFunding.Services.Core.Options;
using System;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Models.Specs.Messages;
using CalculateFunding.Models.Exceptions;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Interfaces.EventHub;
using Microsoft.Azure.EventHubs;

namespace CalculateFunding.Services.Specs
{
    public class SpecificationsService : ISpecificationsService
    {
        private readonly IMapper _mapper;
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly ILogger _logger;
        private readonly IValidator<PolicyCreateModel> _policyCreateModelValidator;
        private readonly IValidator<SpecificationCreateModel> _specificationCreateModelvalidator;
        private readonly IValidator<CalculationCreateModel> _calculationCreateModelValidator;
        private readonly IMessengerService _messengerService;
        private readonly EventHubSettings _eventHubSettings;
        private readonly ISearchRepository<SpecificationIndex> _searchRepository;
        private readonly IValidator<AssignDefinitionRelationshipMessage> _assignDefinitionRelationshipMessageValidator;

        const string createDraftcalculationSubscription = "calc-events-create-draft";
        
        public SpecificationsService(IMapper mapper, 
            ISpecificationsRepository specificationsRepository, ILogger logger, IValidator<PolicyCreateModel> policyCreateModelValidator,
            IValidator<SpecificationCreateModel> specificationCreateModelvalidator, IValidator<CalculationCreateModel> calculationCreateModelValidator,
            IMessengerService messengerService, EventHubSettings eventHubSettings, ISearchRepository<SpecificationIndex> searchRepository,
            IValidator<AssignDefinitionRelationshipMessage> assignDefinitionRelationshipMessageValidator)
        {
            _mapper = mapper;
            _specificationsRepository = specificationsRepository;
            _logger = logger;
            _policyCreateModelValidator = policyCreateModelValidator;
            _specificationCreateModelvalidator = specificationCreateModelvalidator;
            _calculationCreateModelValidator = calculationCreateModelValidator;
            _messengerService = messengerService;
            _eventHubSettings = eventHubSettings;
            _searchRepository = searchRepository;
            _assignDefinitionRelationshipMessageValidator = assignDefinitionRelationshipMessageValidator;
        }

        public async Task<IActionResult> GetSpecificationById(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out var specId);

            var specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetSpecificationById");

                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            Specification specification = await _specificationsRepository.GetSpecificationById(specificationId);

            if (specification == null)
            {
                _logger.Warning($"A specification for id {specificationId} could not found");

                return new NotFoundResult();
            }

            return new OkObjectResult(specification);
        }

        public async Task<IActionResult> GetSpecificationByAcademicYearId(HttpRequest request)
        {
            request.Query.TryGetValue("academicYearId", out var yearId);

            var academicYearId = yearId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(academicYearId))
            {
                _logger.Error("No academic year Id was provided to GetSpecificationByAcademicYearId");

                return new BadRequestObjectResult("Null or empty academicYearId provided");
            }

            IEnumerable<Specification> specifications = await _specificationsRepository.GetSpecificationsByQuery(m => m.AcademicYear.Id == academicYearId);

            if (specifications.IsNullOrEmpty())
            {
                _logger.Information($"No specifications found for academic year with id {academicYearId}");

                return new OkObjectResult(new Specification[0]);
            }

            _logger.Information($"Found {specifications.Count()} specifications for academic year with id {academicYearId}");

            return new OkObjectResult(specifications);
        }

        public async Task<IActionResult> GetSpecificationByName(HttpRequest request)
        {
            request.Query.TryGetValue("specificationName", out var specName);

            var specificationName = specName.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specName))
            {
                _logger.Error("No specification name was provided to GetSpecificationByName");

                return new BadRequestObjectResult("Null or empty specification name provided");
            }

            IEnumerable<Specification> specifications = await _specificationsRepository.GetSpecificationsByQuery(m => m.Name.ToLower() == specificationName.ToLower());

            if (!specifications.Any())
            {
                _logger.Information($"Specification was not found for name: {specificationName}");

                return new NotFoundResult();
            }

            _logger.Information($"Specification found for name: {specificationName}");

            return new OkObjectResult(specifications.FirstOrDefault());
        }

        public async Task<IActionResult> GetPolicyByName(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            PolicyGetModel model = JsonConvert.DeserializeObject<PolicyGetModel>(json);

            if (string.IsNullOrWhiteSpace(model.SpecificationId))
            {
                _logger.Error("No specification id was provided to GetPolicyByName");
                return new BadRequestObjectResult("Null or empty specification id provided");
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                _logger.Error("No policy name was provided to GetPolicyByName");
                return new BadRequestObjectResult("Null or empty policy name provided");
            }

            Specification specification = await _specificationsRepository.GetSpecificationById(model.SpecificationId);

            if (specification == null)
            {
                _logger.Error($"No specification was found for specification id {model.SpecificationId}");
                return new StatusCodeResult(412);
            }

            Policy policy = specification.GetPolicyByName(model.Name);

            if (policy != null)
            {
                _logger.Information($"A policy was found for specification id {model.SpecificationId} and name {model.Name}");

                return new OkObjectResult(policy);
            }

            _logger.Information($"A policy was not found for specification id {model.SpecificationId} and name {model.Name}");

            return new NotFoundResult();
        }

        public async Task<IActionResult> GetCalculationByName(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            CalculationGetModel model = JsonConvert.DeserializeObject<CalculationGetModel>(json);

            if (string.IsNullOrWhiteSpace(model.SpecificationId))
            {
                _logger.Error("No specification id was provided to GetCalculationByName");
                return new BadRequestObjectResult("Null or empty specification id provided");
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                _logger.Error("No calculation name was provided to GetCalculationByName");
                return new BadRequestObjectResult("Null or empty calculation name provided");
            }

            Calculation calculation = await _specificationsRepository.GetCalculationBySpecificationIdAndCalculationName(model.SpecificationId, model.Name);

            if (calculation != null)
            {
                _logger.Information($"A calculation was found for specification id {model.SpecificationId} and name {model.Name}");

                return new OkObjectResult(calculation);
            }

            _logger.Information($"A calculation was not found for specification id {model.SpecificationId} and name {model.Name}");

            return new NotFoundResult();
        }

        public async Task<IActionResult> GetCalculationBySpecificationIdAndCalculationId(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out var specId);

            var specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetCalculationBySpecificationIdAndCalculationId");

                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            request.Query.TryGetValue("calculationId", out var calcId);

            var calculationId = calcId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(calculationId))
            {
                _logger.Error("No calculation Id was provided to GetCalculationBySpecificationIdAndCalculationId");

                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            Calculation calculation = await _specificationsRepository.GetCalculationBySpecificationIdAndCalculationId(specificationId, calculationId);

            if (calculation != null)
            {
                _logger.Information($"A calculation was found for specification id {specificationId} and calculation id {calculationId}");

                return new OkObjectResult(calculation);
            }

            _logger.Information($"A calculation was not found for specification id {specificationId} and calculation id {calculationId}");

            return new NotFoundResult();
        }

        public async Task<IActionResult> GetAcademicYears(HttpRequest request)
        {
            IEnumerable<AcademicYear> academicYears = await _specificationsRepository.GetAcademicYears();

            if (academicYears.IsNullOrEmpty())
            {
                _logger.Error($"No academic years were returned");

                academicYears = new AcademicYear[0];
            }

            return new OkObjectResult(academicYears);
        }

        public async Task<IActionResult> GetFundingStreams(HttpRequest request)
        {
            IEnumerable<FundingStream> fundingStreams = await _specificationsRepository.GetFundingStreams();

            if (fundingStreams.IsNullOrEmpty())
            {
                _logger.Error($"No academic years were returned");

                fundingStreams = new FundingStream[0];
            }

            return new OkObjectResult(fundingStreams);
        }

        public async Task<IActionResult> GetAllocationLines(HttpRequest request)
        {
            IEnumerable<AllocationLine> allocationLines = await _specificationsRepository.GetAllocationLines();
            return new OkObjectResult(allocationLines);
        }

        public async Task<IActionResult> CreatePolicy(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            PolicyCreateModel createModel = JsonConvert.DeserializeObject<PolicyCreateModel>(json);

            if (createModel == null)
                return new BadRequestObjectResult("Null policy create model provided");

            var validationResult = (await _policyCreateModelValidator.ValidateAsync(createModel)).PopulateModelState();

            if (validationResult != null)
                return validationResult;

            Specification specification = await _specificationsRepository.GetSpecificationById(createModel.SpecificationId);

            if (specification == null)
                return new NotFoundResult();

            Policy policy = _mapper.Map<Policy>(createModel);

            if (!string.IsNullOrWhiteSpace(createModel.ParentPolicyId))
            {
                Policy parentPolicy = specification.GetPolicy(createModel.ParentPolicyId);

                parentPolicy.SubPolicies = (parentPolicy.SubPolicies == null
                    ? new[] { policy }
                    : parentPolicy.SubPolicies.Concat(new[] { policy }));
            }
            else
            {
                specification.Policies = (specification.Policies == null
                   ? new[] { policy }
                   : specification.Policies.Concat(new[] { policy }));
            }

            var statusCode = await _specificationsRepository.UpdateSpecification(specification);

            if(statusCode != HttpStatusCode.OK)
                 return new StatusCodeResult((int)statusCode);

            return new OkObjectResult(policy);
        }

        public async Task<IActionResult> CreateSpecification(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            SpecificationCreateModel createModel = JsonConvert.DeserializeObject<SpecificationCreateModel>(json);

            if (createModel == null)
                return new BadRequestObjectResult("Null policy create model provided");

            var validationResult = (await _specificationCreateModelvalidator.ValidateAsync(createModel)).PopulateModelState();

            if (validationResult != null)
                return validationResult;

            AcademicYear academicYear = await _specificationsRepository.GetAcademicYearById(createModel.AcademicYearId);

            FundingStream fundingStream = await _specificationsRepository.GetFundingStreamById(createModel.FundingStreamId);

            Specification specification = _mapper.Map<Specification>(createModel);

            specification.AcademicYear = new Reference(academicYear.Id, academicYear.Name);

            specification.FundingStream = new Reference(fundingStream.Id, fundingStream.Name);

            var statusCode = await _specificationsRepository.CreateSpecification(specification);

            if (!statusCode.IsSuccess())
                return new StatusCodeResult((int)statusCode);

            await _searchRepository.Index(new List<SpecificationIndex>
            {
                new SpecificationIndex
                {
                    Id = specification.Id,
                    Name = specification.Name,
                    FundingStreamId = specification.FundingStream.Id,
                    FundingStreamName = specification.FundingStream.Name,
                    PeriodId = specification.AcademicYear.Id,
                    PeriodName = specification.AcademicYear.Name,
                    LastUpdatedDate = DateTimeOffset.Now
                }
            });  
            
            return new OkObjectResult(specification);
        }

        public async Task<IActionResult> CreateCalculation(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            CalculationCreateModel createModel = JsonConvert.DeserializeObject<CalculationCreateModel>(json);

            if (createModel == null)
            {
                _logger.Error("Null calculation create model provided to CreateCalculation");

                return new BadRequestObjectResult("Null calculation create model provided");
            }

            var validationResult = (await _calculationCreateModelValidator.ValidateAsync(createModel)).PopulateModelState();

            if (validationResult != null)
            {
                _logger.Error("Invalid data was provided for CreateCalculation");

                return validationResult;
            }

            Specification specification = await _specificationsRepository.GetSpecificationById(createModel.SpecificationId);

            if (specification == null)
            {
                _logger.Warning($"Specification not found for specification id {createModel.SpecificationId}");
                return new StatusCodeResult(412);
            }

            Calculation calculation = _mapper.Map<Calculation>(createModel);

            Policy policy = specification.GetPolicy(createModel.PolicyId);

            if (policy == null)
            {
                _logger.Warning($"Policy not found for policy id {createModel.PolicyId}");
                return new StatusCodeResult(412);
            }

            calculation.AllocationLine = await _specificationsRepository.GetAllocationLineById(createModel.AllocationLineId);

            policy.Calculations = (policy.Calculations == null
                ? new[] { calculation }
                : policy.Calculations.Concat(new[] { calculation }));

            var statusCode = await _specificationsRepository.UpdateSpecification(specification);

            if (statusCode != HttpStatusCode.OK)
            {
                _logger.Error($"Failed to update specification when creating a calc with status {statusCode}");

                return new StatusCodeResult((int)statusCode);
            }

            IDictionary<string, string> properties = CreateMessageProperties(request);

            await _messengerService.SendAsync(createDraftcalculationSubscription, 
                new Models.Calcs.Calculation
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = calculation.Name,
                    CalculationSpecification = new Reference(calculation.Id, calculation.Name),
                    AllocationLine = calculation.AllocationLine,
                    Policies = new List<Reference>
                    {
                        new Reference( policy.Id, policy.Name )
                    },
                    Specification = new SpecificationSummary
                    {
	                    Id = specification.Id,
						Name = specification.Name,
						FundingStream = specification.FundingStream,
						Period = specification.AcademicYear
                    },
                    Period = specification.AcademicYear,
                    FundingStream = specification.FundingStream
                }, 
                properties);

            return new OkObjectResult(calculation);
        }

        public async Task AssignDataDefinitionRelationship(EventData message)
        {
            AssignDefinitionRelationshipMessage relationshipMessage = message.GetPayloadAsInstanceOf<AssignDefinitionRelationshipMessage>();

            if (relationshipMessage == null)
            {
                _logger.Error("A null relationship message was provided to AssignDataDefinitionRelationship");

                throw new ArgumentNullException(nameof(relationshipMessage));
            }
            else
            {
                var validationResult = await _assignDefinitionRelationshipMessageValidator.ValidateAsync(relationshipMessage);

                if (!validationResult.IsValid)
                {
                    throw new InvalidModelException(GetType().ToString(), validationResult.Errors.Select(m => m.ErrorMessage).ToArraySafe());
                }

                string specificationId = relationshipMessage.SpecificationId;

                string relationshipId = relationshipMessage.RelationshipId;

                Specification specification = await _specificationsRepository.GetSpecificationById(specificationId);

                if(specification == null)
                {
                    throw new InvalidModelException(relationshipMessage.GetType().ToString(), new[] {  $"Specification could not be found for id {specificationId}" });
                }

                if (specification.DataDefinitionRelationshipIds.IsNullOrEmpty())
                    specification.DataDefinitionRelationshipIds = new string[0];

                if(!specification.DataDefinitionRelationshipIds.Contains(relationshipId))
                    specification.DataDefinitionRelationshipIds = specification.DataDefinitionRelationshipIds.Concat(new[] { relationshipId });

                HttpStatusCode status = await _specificationsRepository.UpdateSpecification(specification);

                if(!status.IsSuccess())
                {
                    _logger.Error($"Failed to update specification for id: {specificationId} with dataset definition relationship id {relationshipId}");

                    throw new Exception($"Failed to update specification for id: {specificationId} with dataset definition relationship id {relationshipId}");
                }

                SpecificationIndex specIndex = await _searchRepository.SearchById(specificationId);

                if(specIndex == null)
                {
                    specIndex = new SpecificationIndex
                    {
                        Id = specification.Id,
                        Name = specification.Name,
                        FundingStreamId = specification.FundingStream.Id,
                        FundingStreamName = specification.FundingStream.Name,
                        PeriodId = specification.AcademicYear.Id,
                        PeriodName = specification.AcademicYear.Name,
                        LastUpdatedDate = DateTimeOffset.Now
                    };
                }

                specIndex.DataDefinitionRelationshipIds = specification.DataDefinitionRelationshipIds.ToArraySafe();

                IList<IndexError> errors = await _searchRepository.Index(new List<SpecificationIndex>{specIndex});

                if (errors.Any())
                {
                    _logger.Error($"failed to index search with the following errors: {string.Join(";", errors.Select(m => m.ErrorMessage))}");

                    throw new FailedToIndexSearchException(errors);
                }

                _logger.Information($"Succeffuly assigned relationship id: {relationshipId} to specification with id: {specificationId}");
            }
        }

        public async Task<IActionResult> ReIndex()
        {
            try
            {
                await _searchRepository.DeleteIndex();

                const string sql = "select s.id, s.content.name, s.content.fundingStream, s.content.academicYear, s.content.dataDefinitionRelationshipIds, s.updatedAt from specs s";

                IEnumerable<SpecificationSearchModel> specifications = (await _specificationsRepository.GetSpecificationsByRawQuery<SpecificationSearchModel>(sql)).ToArraySafe();

                List<SpecificationIndex> specDocuments = new List<SpecificationIndex>();

                foreach (SpecificationSearchModel specification in specifications)
                {
                    specDocuments.Add(new SpecificationIndex
                    {
                        Id = specification.Id,
                        Name = specification.Name,
                        FundingStreamId = specification.FundingStream.Id,
                        FundingStreamName = specification.FundingStream.Name,
                        PeriodId = specification.AcademicYear.Id,
                        PeriodName = specification.AcademicYear.Name,
                        LastUpdatedDate = specification.UpdatedAt,
                        DataDefinitionRelationshipIds = specification.DataDefinitionRelationshipIds.IsNullOrEmpty() ? new string[0] : specification.DataDefinitionRelationshipIds
                    });
                }

                if (!specDocuments.IsNullOrEmpty())
                {
                    await _searchRepository.Index(specDocuments);
                    _logger.Information($"Succesfully re-indexed {specifications.Count()} documents");
                }
                else
                    _logger.Warning("No specification documents were returned from cosmos db");

                return new NoContentResult();
            }
            catch(Exception exception)
            {
                _logger.Error(exception, "Failed re-indexing specifications");

                return new StatusCodeResult(500);
            }
        }

        IDictionary<string, string> CreateMessageProperties(HttpRequest request)
        {
            Reference user = request.GetUser();

            IDictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add("sfa-correlationId", request.GetCorrelationId());

            if (user != null)
            {
                properties.Add("user-id", user.Id);
                properties.Add("user-name", user.Name);
            }

            return properties;
        }

    }
}
