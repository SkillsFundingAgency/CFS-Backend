using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy.TemplateBuilder;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Services.Policy.Models;
using CalculateFunding.Services.Policy.Validators;
using FluentValidation.Results;
using Serilog;

namespace CalculateFunding.Services.Policy.TemplateBuilder
{
    public class TemplateBuilderService : ITemplateBuilderService, IHealthChecker
    {
        private readonly IIoCValidatorFactory _validatorFactory;
        private readonly IFundingTemplateValidationService _fundingTemplateValidationService;
        private readonly ITemplateMetadataResolver _templateMetadataResolver;
        private readonly ITemplateVersionRepository _templateVersionRepository;
        private readonly ILogger _logger;
        private readonly ITemplateRepository _templateRepository;

        public TemplateBuilderService(
            IIoCValidatorFactory validatorFactory,
            IFundingTemplateValidationService fundingTemplateValidationService,
            ITemplateMetadataResolver templateMetadataResolver,
            ITemplateVersionRepository templateVersionRepository,
            ITemplateRepository templateRepository,
            ILogger logger)
        {
            _validatorFactory = validatorFactory;
            _fundingTemplateValidationService = fundingTemplateValidationService;
            _templateMetadataResolver = templateMetadataResolver;
            _templateVersionRepository = templateVersionRepository;
            _templateRepository = templateRepository;
            _logger = logger;
        }
        
        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth templateRepoHealth = await ((IHealthChecker)_templateRepository).IsHealthOk();
            ServiceHealth templateVersionRepoHealth = await ((IHealthChecker)_templateVersionRepository).IsHealthOk();

            ServiceHealth health = new ServiceHealth
            {
                Name = GetType().Name
            };
            health.Dependencies.AddRange(templateRepoHealth.Dependencies);
            health.Dependencies.AddRange(templateVersionRepoHealth.Dependencies);

            return health;
        }

        public async Task<TemplateResponse> GetTemplate(string templateId)
        {
            Guard.IsNotEmpty(templateId, nameof(templateId));
            
            var template = await _templateRepository.GetTemplate(templateId);
            if (template == null)
            {
                return null;
            }

            return Map(template.Current);
        }

        public async Task<TemplateResponse> GetTemplateVersion(string templateId, string version)
        {
            Guard.IsNotEmpty(templateId, nameof(templateId));
            if (!int.TryParse(version, out int versionNumber))
                return null;
            
            var templateVersion = await _templateVersionRepository.GetTemplateVersion(templateId, versionNumber);
            if (templateVersion == null)
            {
                return null;
            }

            return Map(templateVersion);
        }

        public async Task<IEnumerable<TemplateVersionResponse>> GetTemplateVersions(string templateId, List<TemplateStatus> statuses)
        {
            Guard.ArgumentNotNull(templateId, nameof(templateId));

            IEnumerable<TemplateVersion> templateVersions = await _templateVersionRepository.GetVersions(templateId);

            if (statuses.Any())
            {
                templateVersions = templateVersions.Where(v => statuses.Contains(v.Status));
            }

            return templateVersions.Select(s => new TemplateVersionResponse
            {
                Date = s.Date,
                AuthorId = s.Author?.Id,
                AuthorName = s.Author?.Name,
                Comment = s.Comment,
                Status = s.Status,
                Version = s.Version
            }).ToList();
        }

        public async Task<CreateTemplateResponse> CreateTemplate(
            TemplateCreateCommand command,
            Reference author)
        {
            ValidationResult validatorResult = _validatorFactory.Validate(command).And(_validatorFactory.Validate(author));
            if (!validatorResult.IsValid)
            {
                return new CreateTemplateResponse
                {
                    ValidationResult = validatorResult
                };
            }

            try
            {
                if (await _templateRepository.IsTemplateNameInUse(command.Name))
                {
                    string validationErrorMessage = $"Template name [{command.Name}] already in use";
                    _logger.Error(validationErrorMessage);
                    ValidationResult validationResult = new ValidationResult();
                    validationResult.Errors.Add(new ValidationFailure(nameof(command.Name), validationErrorMessage));
                    return new CreateTemplateResponse
                    {
                        ErrorMessage = validationErrorMessage,
                        ValidationResult = validationResult
                    };
                }

                Template template = new Template
                {
                    TemplateId = Guid.NewGuid().ToString()
                };
                template.Current = new TemplateVersion
                {
                    TemplateId = template.TemplateId,
                    FundingStreamId = command.FundingStreamId,
                    Name = command.Name,
                    Description = command.Description,
                    Version = 0,
                    PublishStatus = PublishStatus.Draft,
                    SchemaVersion = command.SchemaVersion,
                    Author = author,
                    Date = DateTimeOffset.Now.ToLocalTime()
                };

                HttpStatusCode result = await _templateRepository.CreateDraft(template);

                if (result.IsSuccess())
                {
                    await _templateVersionRepository.SaveVersion(template.Current);

                    return new CreateTemplateResponse
                    {
                        Succeeded = true,
                        TemplateId = template.TemplateId
                    };
                }

                string errorMessage = $"Failed to create a new template with name {command.Name} in Cosmos. Status code {(int) result}";
                _logger.Error(errorMessage);

                return new CreateTemplateResponse
                {
                    ErrorMessage = errorMessage
                };
            }
            catch (Exception ex)
            {
                return new CreateTemplateResponse
                {
                    Exception = ex
                };
            }
        }

        public async Task<UpdateTemplateContentResponse> UpdateTemplateContent(TemplateContentUpdateCommand command, Reference author)
        {
            // input parameter validation
            var validatorResult = _validatorFactory.Validate(command).And(_validatorFactory.Validate(author));
            if (!validatorResult.IsValid)
            {
                return UpdateTemplateContentResponse.ValidationFail(validatorResult);
            }
            
            var template = await _templateRepository.GetTemplate(command.TemplateId);
            if (template == null)
            {
                return UpdateTemplateContentResponse.ValidationFail(nameof(command.TemplateId), "Template doesn't exist");
            }

            if (template.Current.TemplateJson == command.TemplateJson)
                return UpdateTemplateContentResponse.Success();

            UpdateTemplateContentResponse validationError = await ValidateTemplateContent(command);
            if (validationError != null)
                return validationError;

            await UpdateTemplateContent(command, author, template);

            return UpdateTemplateContentResponse.Success();
        }

        public async Task<UpdateTemplateMetadataResponse> UpdateTemplateMetadata(TemplateMetadataUpdateCommand command, Reference author)
        {
            var validatorResult = _validatorFactory.Validate(command).And(_validatorFactory.Validate(author));
            if (!validatorResult.IsValid)
            {
                return UpdateTemplateMetadataResponse.ValidationFail(validatorResult);
            }
            
            var template = await _templateRepository.GetTemplate(command.TemplateId);
            if (template == null)
            {
                return UpdateTemplateMetadataResponse.ValidationFail(nameof(command.TemplateId), "Template doesn't exist");
            }

            if (template.Current.Name == command.Name && template.Current.Description == command.Description)
                return UpdateTemplateMetadataResponse.Success();

            // validate template name is unique if it is changing
            if (template.Current.Name != command.Name)
            {
                if (await _templateRepository.IsTemplateNameInUse(command.Name))
                {
                    string validationErrorMessage = $"Template name [{command.Name}] already in use";
                    _logger.Error(validationErrorMessage);
                    ValidationResult validationResult = new ValidationResult();
                    validationResult.Errors.Add(new ValidationFailure(nameof(command.Name), validationErrorMessage));
                    return new UpdateTemplateMetadataResponse
                    {
                        ErrorMessage = validationErrorMessage,
                        ValidationResult = validationResult
                    };
                }
            }

            await UpdateTemplateMetadata(command, author, template);

            return UpdateTemplateMetadataResponse.Success();
        }

        private static TemplateResponse Map(TemplateVersion template)
        {
            return new TemplateResponse
            {
                TemplateId = template.TemplateId,
                TemplateJson = template.TemplateJson,
                Name = template.Name,
                Description = template.Description,
                FundingStreamId = template.FundingStreamId,
                Version = template.Version,
                SchemaVersion = template.SchemaVersion,
                Status = template.Status,
                AuthorId = template.Author.Id,
                AuthorName = template.Author.Name,
                LastModificationDate = template.Date.DateTime,
                PublishStatus = template.PublishStatus,
                Comments = template.Comment
            };
        }

        private async Task UpdateTemplateContent(TemplateContentUpdateCommand command, Reference author, Template template)
        {
            // create new version and save it
            TemplateVersion newVersion = template.Current.Clone() as TemplateVersion;
            newVersion.Author = author;
            newVersion.TemplateJson = command.TemplateJson;
            newVersion.Version++;
            newVersion.Predecessors ??= new List<string>();
            newVersion.Predecessors.Add(template.Current.Id);
            await _templateVersionRepository.SaveVersion(newVersion);
            
            // update template
            template.AddPredecessor(template.Current.Id);
            template.Current = newVersion;
            await _templateRepository.Update(template);
        }

        private async Task UpdateTemplateMetadata(TemplateMetadataUpdateCommand command, Reference author, Template template)
        {
            // create new version and save it
            TemplateVersion newVersion = template.Current.Clone() as TemplateVersion;
            newVersion.Author = author;
            newVersion.Name = command.Name;
            newVersion.Description = command.Description;
            newVersion.Version++;
            newVersion.Predecessors ??= new List<string>();
            newVersion.Predecessors.Add(template.Current.Id);
            await _templateVersionRepository.SaveVersion(newVersion);
            
            // update template
            template.AddPredecessor(template.Current.Id);
            template.Current = newVersion;
            await _templateRepository.Update(template);
        }

        private async Task<UpdateTemplateContentResponse> ValidateTemplateContent(TemplateContentUpdateCommand command)
        {
            // template json validation
            FundingTemplateValidationResult validationResult = await _fundingTemplateValidationService.ValidateFundingTemplate(command.TemplateJson);

            if (!validationResult.IsValid)
            {
                return UpdateTemplateContentResponse.ValidationFail(validationResult.ValidationState);
            }

            // schema specific validation
            ITemplateMetadataGenerator templateMetadataGenerator = _templateMetadataResolver.GetService(validationResult.SchemaVersion);

            ValidationResult validationGeneratorResult = templateMetadataGenerator.Validate(command.TemplateJson);

            if (!validationGeneratorResult.IsValid)
            {
                return UpdateTemplateContentResponse.ValidationFail(validationGeneratorResult);
            }

            return null;
        }
    }
}