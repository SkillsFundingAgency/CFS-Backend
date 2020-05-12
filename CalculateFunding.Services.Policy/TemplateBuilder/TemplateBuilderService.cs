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

        public async Task<IEnumerable<TemplateResponse>> GetTemplateVersions(string templateId, List<TemplateStatus> statuses)
        {
            Guard.ArgumentNotNull(templateId, nameof(templateId));

            IEnumerable<TemplateVersion> templateVersions = await _templateVersionRepository.GetTemplateVersions(templateId, statuses);

            return templateVersions.Select(Map).ToList();
        }

        public async Task<CommandResult> CreateTemplate(
            TemplateCreateCommand command,
            Reference author)
        {
            ValidationResult validatorResult = _validatorFactory.Validate(command).And(_validatorFactory.Validate(author));
            if (!validatorResult.IsValid)
            {
                return new CommandResult
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
                    return new CommandResult
                    {
                        ErrorMessage = validationErrorMessage,
                        ValidationResult = validationResult
                    };
                }

                if (await _templateRepository.IsFundingStreamAndPeriodInUse(command.FundingStreamId, command.FundingPeriodId))
                {
                    string validationErrorMessage = $"Template with FundingStreamId [{command.FundingStreamId}] and FundingPeriodId [{command.FundingPeriodId}] already in use";
                    _logger.Error(validationErrorMessage);
                    ValidationResult validationResult = new ValidationResult();
                    validationResult.Errors.Add(new ValidationFailure(nameof(command.FundingStreamId), validationErrorMessage));
                    validationResult.Errors.Add(new ValidationFailure(nameof(command.FundingPeriodId), validationErrorMessage));
                    return new CommandResult
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
                    FundingPeriodId = command.FundingPeriodId,
                    Name = command.Name,
                    Description = command.Description,
                    Version = 1,
                    MajorVersion = 0,
                    MinorVersion = 1,
                    PublishStatus = PublishStatus.Draft,
                    SchemaVersion = command.SchemaVersion,
                    Author = author,
                    Date = DateTimeOffset.Now.ToLocalTime()
                };

                HttpStatusCode result = await _templateRepository.CreateDraft(template);

                if (result.IsSuccess())
                {
                    await _templateVersionRepository.SaveVersion(template.Current);

                    return new CommandResult
                    {
                        Succeeded = true,
                        TemplateId = template.TemplateId
                    };
                }

                string errorMessage = $"Failed to create a new template with name {command.Name} in Cosmos. Status code {(int) result}";
                _logger.Error(errorMessage);

                return new CommandResult
                {
                    ErrorMessage = errorMessage
                };
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    Exception = ex
                };
            }
        }

        public async Task<CommandResult> UpdateTemplateContent(TemplateContentUpdateCommand command, Reference author)
        {
            // input parameter validation
            var validatorResult = _validatorFactory.Validate(command).And(_validatorFactory.Validate(author));
            if (!validatorResult.IsValid)
            {
                return CommandResult.ValidationFail(validatorResult);
            }
            
            var template = await _templateRepository.GetTemplate(command.TemplateId);
            if (template == null)
            {
                return CommandResult.ValidationFail(nameof(command.TemplateId), "Template doesn't exist");
            }

            if (template.Current.TemplateJson == command.TemplateJson)
            {
                return CommandResult.Success();
            }

            CommandResult validationError = await ValidateTemplateContent(command);
            if (validationError != null)
                return validationError;

            var updated = await UpdateTemplateContent(command, author, template);

            if (!updated.IsSuccess())
            {
                return CommandResult.Fail($"Failed to update template: {updated}");
            }
            
            return CommandResult.Success();
        }

        public async Task<CommandResult> UpdateTemplateMetadata(TemplateMetadataUpdateCommand command, Reference author)
        {
            var validatorResult = _validatorFactory.Validate(command).And(_validatorFactory.Validate(author));
            if (!validatorResult.IsValid)
            {
                return CommandResult.ValidationFail(validatorResult);
            }
            
            var template = await _templateRepository.GetTemplate(command.TemplateId);
            if (template == null)
            {
                return CommandResult.ValidationFail(nameof(command.TemplateId), "Template doesn't exist");
            }

            if (template.Current.Name == command.Name && template.Current.Description == command.Description)
            {
                return CommandResult.Success();
            }

            // validate template name is unique if it is changing
            if (template.Current.Name != command.Name)
            {
                if (await _templateRepository.IsTemplateNameInUse(command.Name))
                {
                    string validationErrorMessage = $"Template name [{command.Name}] already in use";
                    _logger.Error(validationErrorMessage);
                    ValidationResult validationResult = new ValidationResult();
                    validationResult.Errors.Add(new ValidationFailure(nameof(command.Name), validationErrorMessage));
                    return new CommandResult
                    {
                        ErrorMessage = validationErrorMessage,
                        ValidationResult = validationResult
                    };
                }
            }

            var updated = await UpdateTemplateMetadata(command, author, template);

            if (!updated.IsSuccess())
            {
                return CommandResult.Fail($"Failed to update template: {updated}");
            }
            
            return CommandResult.Success();
        }

        public async Task<CommandResult> ApproveTemplate(Reference author, string templateId, string comment, string version = null)
        {
            Guard.IsNotEmpty(templateId, nameof(templateId));
            Guard.ArgumentNotNull(author, nameof(author));
            
            var template = await _templateRepository.GetTemplate(templateId);
            if (template == null)
            {
                return CommandResult.ValidationFail(nameof(templateId), "Template doesn't exist");
            }

            var templateVersion = template.Current;
            if (version != null)
            {
                if (!int.TryParse(version, out int versionNumber))
                {
                    return CommandResult.ValidationFail(nameof(version), $"Invalid version '{version}'");
                }
                templateVersion = await _templateVersionRepository.GetTemplateVersion(templateId, versionNumber);
                if (templateVersion == null)
                {
                    return CommandResult.ValidationFail(nameof(version), $"Version '{version}' could not be found for template '{templateId}'");
                }
            }

            if (templateVersion.Status == TemplateStatus.Published)
            {
                return CommandResult.ValidationFail(nameof(templateVersion.Status), "Template version is already published");
            }

            // create new version and save it
            TemplateVersion newVersion = templateVersion.Clone() as TemplateVersion;
            newVersion.Author = author;
            newVersion.Name = templateVersion.Name;
            newVersion.Description = templateVersion.Description;
            newVersion.Comment = comment;
            newVersion.TemplateJson = templateVersion.TemplateJson;
            newVersion.Status = TemplateStatus.Published;
            newVersion.Version = template.Current.Version + 1;
            newVersion.MajorVersion = template.Current.MajorVersion + 1;
            newVersion.MinorVersion = 0;
            newVersion.Date = DateTimeOffset.Now;
            newVersion.Predecessors ??= new List<string>();
            newVersion.Predecessors.Add(template.Current.Id);
            var templateVersionUpdateResult = await _templateVersionRepository.SaveVersion(newVersion);
            if (!templateVersionUpdateResult.IsSuccess())
            {
                return CommandResult.ValidationFail(nameof(templateId), $"Template version failed to save: {templateVersionUpdateResult}");
            }

            // update template
            template.Name = newVersion.Name;
            template.AddPredecessor(template.Current.Id);
            template.Current = newVersion;
            var templateUpdateResult = await _templateRepository.Update(template);
            if (!templateUpdateResult.IsSuccess())
            {
                return CommandResult.Fail($"Failed to approve template: {templateUpdateResult}");
            }
            
            return CommandResult.Success();
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
                FundingPeriodId = template.FundingPeriodId,
                Version = template.Version,
                MinorVersion = template.MinorVersion,
                MajorVersion = template.MajorVersion,
                SchemaVersion = template.SchemaVersion,
                Status = template.Status,
                AuthorId = template.Author.Id,
                AuthorName = template.Author.Name,
                LastModificationDate = template.Date.DateTime,
                PublishStatus = template.PublishStatus,
                Comments = template.Comment
            };
        }

        private async Task<HttpStatusCode> UpdateTemplateContent(TemplateContentUpdateCommand command, Reference author, Template template)
        {
            // create new version and save it
            TemplateVersion newVersion = template.Current.Clone() as TemplateVersion;
            if (template.Current.Status == TemplateStatus.Published)
            {
                newVersion.Status = TemplateStatus.Draft;
            }
            newVersion.Author = author;
            newVersion.Name = template.Current.Name;
            newVersion.Comment = null;
            newVersion.Description = template.Current.Description;
            newVersion.Status = TemplateStatus.Draft;
            newVersion.Date = DateTimeOffset.Now;
            newVersion.TemplateJson = command.TemplateJson;
            newVersion.Version++;
            newVersion.MinorVersion++;
            newVersion.Predecessors ??= new List<string>();
            newVersion.Predecessors.Add(template.Current.Id);
            await _templateVersionRepository.SaveVersion(newVersion);
            
            // update template
            template.Name = template.Current.Name;
            template.AddPredecessor(template.Current.Id);
            template.Current = newVersion;
            return await _templateRepository.Update(template);
        }

        private async Task<HttpStatusCode> UpdateTemplateMetadata(TemplateMetadataUpdateCommand command, Reference author, Template template)
        {
            // create new version and save it
            TemplateVersion newVersion = template.Current.Clone() as TemplateVersion;
            newVersion.Author = author;
            newVersion.Comment = null;
            newVersion.Status = TemplateStatus.Draft;
            newVersion.Date = DateTimeOffset.Now;
            newVersion.TemplateJson = template.Current.TemplateJson;
            newVersion.Name = command.Name;
            newVersion.Description = command.Description;
            newVersion.Version++;
            newVersion.MinorVersion++;
            newVersion.Predecessors ??= new List<string>();
            newVersion.Predecessors.Add(template.Current.Id);
            var result = await _templateVersionRepository.SaveVersion(newVersion);
            if (!result.IsSuccess())
                return result;
            
            // update template
            template.Name = template.Current.Name;
            template.AddPredecessor(template.Current.Id);
            template.Current = newVersion;
            return await _templateRepository.Update(template);
        }

        private async Task<CommandResult> ValidateTemplateContent(TemplateContentUpdateCommand command)
        {
            // template json validation
            FundingTemplateValidationResult validationResult = await _fundingTemplateValidationService.ValidateFundingTemplate(command.TemplateJson);

            if (!validationResult.IsValid)
            {
                return CommandResult.ValidationFail(validationResult.ValidationState);
            }

            // schema specific validation
            ITemplateMetadataGenerator templateMetadataGenerator = _templateMetadataResolver.GetService(validationResult.SchemaVersion);

            ValidationResult validationGeneratorResult = templateMetadataGenerator.Validate(command.TemplateJson);

            if (!validationGeneratorResult.IsValid)
            {
                return CommandResult.ValidationFail(validationGeneratorResult);
            }

            return null;
        }
    }
}