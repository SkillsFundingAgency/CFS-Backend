using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Calcs
{
    public class CreateCalculationService : ICreateCalculationService
    {
        private readonly IValidator<CalculationCreateModel> _calculationCreateModelValidator;
        private readonly ICalculationNameInUseCheck _calculationNameInUseCheck;
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly IVersionRepository<CalculationVersion> _calculationVersionRepository;
        private readonly Policy _calculationVersionsRepositoryPolicy;
        private readonly Policy _calculationRepositoryPolicy;
        private readonly Policy _cachePolicy;
        private readonly ICacheProvider _cacheProvider;
        private readonly ISearchRepository<CalculationIndex> _searchRepository;
        private readonly ILogger _logger;
        private readonly IInstructionAllocationJobCreation _instructionAllocationJobCreation;

        public CreateCalculationService(ICalculationNameInUseCheck calculationNameInUseCheck, 
            ICalculationsRepository calculationsRepository, 
            IVersionRepository<CalculationVersion> calculationVersionRepository, 
            ICalcsResiliencePolicies calculationsResiliencePolicies, 
            IValidator<CalculationCreateModel> calculationCreateModelValidator,
            ICacheProvider cacheProvider,
            ISearchRepository<CalculationIndex> searchRepository,
            ILogger logger, 
            IInstructionAllocationJobCreation instructionAllocationJobCreation)
        {
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(calculationNameInUseCheck, nameof(calculationNameInUseCheck));
            Guard.ArgumentNotNull(calculationsRepository, nameof(calculationsRepository));
            Guard.ArgumentNotNull(calculationVersionRepository, nameof(calculationVersionRepository));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(calculationsResiliencePolicies?.CacheProviderPolicy, nameof(calculationsResiliencePolicies.CacheProviderPolicy));
            Guard.ArgumentNotNull(calculationsResiliencePolicies?.JobsApiClient, nameof(calculationsResiliencePolicies.JobsApiClient));
            Guard.ArgumentNotNull(calculationsResiliencePolicies?.CalculationsRepository, nameof(calculationsResiliencePolicies.CalculationsRepository));
            Guard.ArgumentNotNull(calculationsResiliencePolicies?.CalculationsVersionsRepositoryPolicy, nameof(calculationsResiliencePolicies.CalculationsVersionsRepositoryPolicy));
            
            _calculationNameInUseCheck = calculationNameInUseCheck;
            _calculationsRepository = calculationsRepository;
            _calculationVersionRepository = calculationVersionRepository;
            _logger = logger;
            _instructionAllocationJobCreation = instructionAllocationJobCreation;
            _searchRepository = searchRepository;
            _cacheProvider = cacheProvider;
            _calculationCreateModelValidator = calculationCreateModelValidator;
            _calculationVersionsRepositoryPolicy = calculationsResiliencePolicies.CalculationsVersionsRepositoryPolicy;
            _calculationRepositoryPolicy = calculationsResiliencePolicies.CalculationsRepository;
            _cachePolicy = calculationsResiliencePolicies.CacheProviderPolicy;
        }

        public async Task<CreateCalculationResponse> CreateCalculation(string specificationId,
            CalculationCreateModel model,
            CalculationNamespace calculationNamespace,
            CalculationType calculationType,
            Reference author,
            string correlationId, 
            bool initiateCalcRun = true)
        {
            
            Guard.ArgumentNotNull(model, nameof(model));
            Guard.ArgumentNotNull(author, nameof(author));

            if (string.IsNullOrWhiteSpace(model.Id))
            {
                model.Id = Guid.NewGuid().ToString();
            }

            model.SpecificationId = specificationId;

            ValidationResult validationResult = await _calculationCreateModelValidator.ValidateAsync(model);

            if (!validationResult.IsValid)
            {
                return new CreateCalculationResponse
                {
                    ValidationResult = validationResult,
                    ErrorType = CreateCalculationErrorType.InvalidRequest
                };
            }

            Calculation calculation = new Calculation
            {
                Id = model.Id,
                FundingStreamId = model.FundingStreamId,
                SpecificationId = model.SpecificationId
            };

            CalculationVersion calculationVersion = new CalculationVersion
            {
                CalculationId = calculation.Id,
                PublishStatus = PublishStatus.Draft,
                Author = author,
                Date = DateTimeOffset.Now.ToLocalTime(),
                Version = 1,
                SourceCode = model.SourceCode,
                Description = model.Description,
                ValueType = model.ValueType.Value,
                CalculationType = calculationType,
                WasTemplateCalculation = false,
                Namespace = calculationNamespace,
                Name = model.Name
            };

            calculation.Current = calculationVersion;

            bool? nameValidResult = await _calculationNameInUseCheck.IsCalculationNameInUse(calculation.SpecificationId, calculation.Name, null);

            if (nameValidResult == true)
            {
                string error =
                    $"Calculation with the same generated source code name already exists in this specification. Calculation Name {calculation.Name} and Specification {calculation.SpecificationId}";
                
                _logger.Error(error);
               
                return new CreateCalculationResponse
                {
                    ErrorMessage = error,
                    ErrorType = CreateCalculationErrorType.InvalidRequest
                };
            }

            calculation.Current.SourceCodeName = VisualBasicTypeGenerator.GenerateIdentifier(calculation.Name);

            HttpStatusCode result = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.CreateDraftCalculation(calculation));

            if (result.IsSuccess())
            {
                await _calculationVersionsRepositoryPolicy.ExecuteAsync(() => _calculationVersionRepository.SaveVersion(calculationVersion));

                await UpdateSearch(calculation);

                string cacheKey = $"{CacheKeys.CalculationsMetadataForSpecification}{specificationId}";

                await _cachePolicy.ExecuteAsync(() => _cacheProvider.RemoveAsync<List<CalculationMetadata>>(cacheKey));

                try
                {
                    Job job = !initiateCalcRun 
                        ? null 
                        : await SendInstructAllocationsToJobService(calculation.SpecificationId, author.Id, author.Name, new Trigger
                    {
                        EntityId = calculation.Id,
                        EntityType = nameof(Calculation),
                        Message = $"Saving calculation: '{calculation.Id}' for specification: '{calculation.SpecificationId}'"
                    }, correlationId);

                    if (!initiateCalcRun || job != null)
                    {
                        _logger.Information($"New job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' created with id: '{job.Id}'");

                        return new CreateCalculationResponse
                        {
                            Succeeded = true,
                            Calculation = calculation
                        };
                    }
                    else
                    {
                        string errorMessage = $"Failed to create job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' on specification '{calculation.SpecificationId}'";

                        _logger.Error(errorMessage);

                        return new CreateCalculationResponse
                        {
                            ErrorType = CreateCalculationErrorType.Exception,
                            ErrorMessage = errorMessage
                        };
                    }
                }
                catch (Exception ex)
                {
                    return new CreateCalculationResponse
                    {
                        ErrorMessage = ex.Message,
                        ErrorType = CreateCalculationErrorType.Exception
                    };
                }
            }
            else
            {
                string errorMessage = $"There was problem creating a new calculation with name {calculation.Name} in Cosmos Db with status code {(int)result}";

                _logger.Error(errorMessage);
                
                return new CreateCalculationResponse
                {
                    ErrorMessage   = errorMessage,
                    ErrorType = CreateCalculationErrorType.Exception
                };
            }
        }
        
        private async Task UpdateSearch(Calculation calculation)
        {
            await _searchRepository.Index(new []
            {
                new CalculationIndex
                {
                    Id = calculation.Id,
                    SpecificationId = calculation.SpecificationId,
                    Name = calculation.Current.Name,
                    ValueType = calculation.Current.ValueType.ToString(),
                    FundingStreamId = calculation.FundingStreamId,
                    Namespace = calculation.Current.Namespace.ToString(),
                    CalculationType = calculation.Current.CalculationType.ToString(),
                    Description = calculation.Current.Description,
                    WasTemplateCalculation = calculation.Current.WasTemplateCalculation,
                    Status = calculation.Current.PublishStatus.ToString(),
                    LastUpdatedDate = calculation.Current.Date
                }
            });
        }
        
        private async Task<Job> SendInstructAllocationsToJobService(string specificationId, string userId, string userName, Trigger trigger, string correlationId)
        {
            return await _instructionAllocationJobCreation.SendInstructAllocationsToJobService(specificationId, userId, userName, trigger, correlationId);
        }
    }
}