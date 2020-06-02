using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Common.TemplateMetadata.Schema11.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Policy.TemplateBuilder;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Services.Policy.Models;
using CalculateFunding.Services.Policy.Validators;
using FluentValidation.Results;
using Newtonsoft.Json;
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
        private readonly ISearchRepository<TemplateIndex> _searchRepository;
        private readonly IPolicyRepository _policyRepository;

        public TemplateBuilderService(
            IIoCValidatorFactory validatorFactory,
            IFundingTemplateValidationService fundingTemplateValidationService,
            ITemplateMetadataResolver templateMetadataResolver,
            ITemplateVersionRepository templateVersionRepository,
            ITemplateRepository templateRepository,
            ISearchRepository<TemplateIndex> searchRepository,
            IPolicyRepository policyRepository,
            ILogger logger)
        {
            _validatorFactory = validatorFactory;
            _fundingTemplateValidationService = fundingTemplateValidationService;
            _templateMetadataResolver = templateMetadataResolver;
            _templateVersionRepository = templateVersionRepository;
            _templateRepository = templateRepository;
            _searchRepository = searchRepository;
            _policyRepository = policyRepository;
            _logger = logger;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth templateRepoHealth = await ((IHealthChecker) _templateRepository).IsHealthOk();
            ServiceHealth templateVersionRepoHealth = await _templateVersionRepository.IsHealthOk();
            (bool Ok, string Message) = await _searchRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth
            {
                Name = GetType().Name
            };
            health.Dependencies.AddRange(templateRepoHealth.Dependencies);
            health.Dependencies.AddRange(templateVersionRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = Ok, DependencyName = _searchRepository.GetType().GetFriendlyName(), Message = Message });

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

        public async Task<IEnumerable<TemplateResponse>> GetVersionsByTemplate(string templateId, List<TemplateStatus> statuses)
        {
            Guard.ArgumentNotNull(templateId, nameof(templateId));

            IEnumerable<TemplateVersion> templateVersions = await _templateVersionRepository.GetByTemplate(templateId, statuses);

            return templateVersions.Select(Map).ToList();
        }

        public async Task<IEnumerable<TemplateResponse>> FindVersionsByFundingStreamAndPeriod(FindTemplateVersionQuery query)
        {
            IEnumerable<TemplateVersion> templateVersions = await _templateVersionRepository
                .FindByFundingStreamAndPeriod(query);

            return templateVersions.Select(Map).ToList();
        }

        public async Task<CommandResult> CreateTemplate(TemplateCreateCommand command, Reference author)
        {
            ValidationResult validatorResult = await _validatorFactory.Validate(command);
            validatorResult.Errors.AddRange((await _validatorFactory.Validate(author))?.Errors);
            
            if (!validatorResult.IsValid)
            {
                return CommandResult.ValidationFail(validatorResult);
            }

            try
            {
                if (await _templateRepository.IsFundingStreamAndPeriodInUse(command.FundingStreamId, command.FundingPeriodId))
                {
                    string validationErrorMessage =
                        $"Template with FundingStreamId [{command.FundingStreamId}] and FundingPeriodId [{command.FundingPeriodId}] already in use";
                    _logger.Error(validationErrorMessage);
                    ValidationResult validationResult = new ValidationResult();
                    validationResult.WithError(nameof(command.FundingPeriodId), validationErrorMessage);
                    validationResult.WithError(nameof(command.FundingStreamId), validationErrorMessage);
                    return new CommandResult
                    {
                        ErrorMessage = validationErrorMessage,
                        ValidationResult = validationResult
                    };
                }

                string templateName = $"{command.FundingStreamId} {command.FundingPeriodId}";
                Template template = new Template
                {
                    TemplateId = Guid.NewGuid().ToString(),
                    Name = templateName,
                    FundingStream = await _policyRepository.GetFundingStreamById(command.FundingStreamId),
                    FundingPeriod = await _policyRepository.GetFundingPeriodById(command.FundingPeriodId)
                };
                template.Current = new TemplateVersion
                {
                    TemplateId = template.TemplateId,
                    FundingStreamId = command.FundingStreamId,
                    FundingPeriodId = command.FundingPeriodId,
                    Name = templateName,
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
                    await CreateTemplateIndexItem(template, author);

                    return new CommandResult
                    {
                        Succeeded = true,
                        TemplateId = template.TemplateId
                    };
                }

                string errorMessage = $"Failed to create a new template with name {templateName} in Cosmos. Status code {(int) result}";
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

        public async Task<CommandResult> CreateTemplateAsClone(TemplateCreateAsCloneCommand command, Reference author)
        {
            ValidationResult validatorResult = await _validatorFactory.Validate(command);
            validatorResult.Errors.AddRange((await _validatorFactory.Validate(author))?.Errors);
            
            if (!validatorResult.IsValid)
            {
                return CommandResult.ValidationFail(validatorResult);
            }

            try
            {
                var sourceTemplate = await _templateRepository.GetTemplate(command.CloneFromTemplateId);
                if (sourceTemplate == null)
                {
                    return CommandResult.ValidationFail(nameof(command.CloneFromTemplateId), "Template doesn't exist");
                }

                var sourceVersion = sourceTemplate.Current;
                if (command.Version != null)
                {
                    if (!int.TryParse(command.Version, out int versionNumber))
                    {
                        return CommandResult.ValidationFail(nameof(command.Version), $"Invalid version '{command.Version}'");
                    }

                    sourceVersion = await _templateVersionRepository.GetTemplateVersion(command.CloneFromTemplateId, versionNumber);
                    if (sourceVersion == null)
                    {
                        return CommandResult.ValidationFail(nameof(command.Version),
                            $"Version '{command.Version}' could not be found for template '{command.CloneFromTemplateId}'");
                    }
                }

                if (await _templateRepository.IsFundingStreamAndPeriodInUse(command.FundingStreamId, command.FundingPeriodId))
                {
                    string validationErrorMessage =
                        $"Template with FundingStreamId [{command.FundingStreamId}] and FundingPeriodId [{command.FundingPeriodId}] already in use";
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

                Guid templateId = Guid.NewGuid();
                string templateName = $"{command.FundingStreamId} {command.FundingPeriodId}";
                Template template = new Template
                {
                    TemplateId = templateId.ToString(), 
                    Name = templateName,
                    FundingStream = await _policyRepository.GetFundingStreamById(command.FundingStreamId),
                    FundingPeriod = await _policyRepository.GetFundingPeriodById(command.FundingPeriodId),
                    Current = new TemplateVersion
                    {
                        TemplateId = templateId.ToString(),
                        SchemaVersion = sourceVersion.SchemaVersion,
                        Author = author,
                        Name = templateName,
                        Description = command.Description,
                        FundingStreamId = command.FundingStreamId,
                        FundingPeriodId = command.FundingPeriodId,
                        Comment = null,
                        TemplateJson = sourceVersion.TemplateJson,
                        Status = TemplateStatus.Draft,
                        Version = 1,
                        MajorVersion = 0,
                        MinorVersion = 1,
                        Date = DateTimeOffset.Now
                    }
                };
                
                // create new version and save it
                HttpStatusCode templateVersionUpdateResult = await _templateVersionRepository.SaveVersion(template.Current);
                if (!templateVersionUpdateResult.IsSuccess())
                {
                    return CommandResult.ValidationFail(nameof(command.Version), $"Template version failed to save: {templateVersionUpdateResult}");
                }

                HttpStatusCode result = await _templateRepository.CreateDraft(template);

                if (result.IsSuccess())
                {
                    await CreateTemplateIndexItem(template, author);
                    
                    return new CommandResult
                    {
                        Succeeded = true,
                        TemplateId = template.TemplateId
                    };
                }

                string errorMessage = $"Failed to create a new template with name {templateName} in Cosmos. Status code {(int) result}";
                _logger.Error(errorMessage);
                return CommandResult.Fail(errorMessage);
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    Exception = ex
                };
            }
        }

        public async Task<CommandResult> UpdateTemplateContent(TemplateFundingLinesUpdateCommand originalCommand, Reference author)
        {
            // input parameter validation
            ValidationResult validatorResult = await _validatorFactory.Validate(originalCommand);
            validatorResult.Errors.AddRange((await _validatorFactory.Validate(author))?.Errors);
            
            if (!validatorResult.IsValid)
            {
                return CommandResult.ValidationFail(validatorResult);
            }

            var template = await _templateRepository.GetTemplate(originalCommand.TemplateId);
            if (template?.Current == null)
            {
                return CommandResult.ValidationFail(nameof(originalCommand.TemplateId), "Template doesn't exist");
            }

            // to enable backwards compatibility
            template.FundingStream ??= await _policyRepository.GetFundingStreamById(template.Current.FundingStreamId);
            template.FundingPeriod ??= await _policyRepository.GetFundingPeriodById(template.Current.FundingPeriodId);

            if (template.Current.TemplateJson == originalCommand.TemplateFundingLinesJson)
            {
                return CommandResult.Success();
            }

            (ValidationFailure error, TemplateJsonContentUpdateCommand updateCommand) = 
                MapCommand(originalCommand, template, TemplateStatus.Draft, 0, 1);
            if (error != null)
            {
                return CommandResult.ValidationFail(new ValidationResult(new []{error}));
            }

            CommandResult validationError = await ValidateTemplateContent(updateCommand.TemplateJson);
            if (validationError != null)
                return validationError;

            var updated = await UpdateTemplateContent(updateCommand, author, template, TemplateStatus.Draft, 0, 1);

            if (!updated.IsSuccess())
            {
                return CommandResult.Fail($"Failed to update template: {updated}");
            }

            return CommandResult.Success();
        }

        public async Task<CommandResult> UpdateTemplateMetadata(TemplateMetadataUpdateCommand command, Reference author)
        {
            ValidationResult validatorResult = await _validatorFactory.Validate(command);
            validatorResult.Errors.AddRange((await _validatorFactory.Validate(author))?.Errors);
            
            if (!validatorResult.IsValid)
            {
                return CommandResult.ValidationFail(validatorResult);
            }

            var template = await _templateRepository.GetTemplate(command.TemplateId);
            if (template?.Current == null)
            {
                return CommandResult.ValidationFail(nameof(command.TemplateId), "Template doesn't exist");
            }

            // to enable backwards compatibility
            template.FundingStream ??= await _policyRepository.GetFundingStreamById(template.Current.FundingStreamId);
            template.FundingPeriod ??= await _policyRepository.GetFundingPeriodById(template.Current.FundingPeriodId);

            if (template.Current.Description == command.Description)
            {
                return CommandResult.Success();
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

        private async Task<HttpStatusCode> UpdateTemplateContent(TemplateJsonContentUpdateCommand command, Reference author, Template template, TemplateStatus status, int majorVersion, int minorVersion)
        {
            // create new version and save it
            TemplateVersion newVersion = template.Current.Clone() as TemplateVersion;
            newVersion.Status = status;
            newVersion.Author = author;
            newVersion.Name = $"{template.FundingStream.Id} {template.FundingPeriod.Id}";
            newVersion.Comment = null;
            newVersion.Description = template.Current.Description;
            newVersion.Status = TemplateStatus.Draft;
            newVersion.Date = DateTimeOffset.Now;
            newVersion.Version = template.Current.Version + 1;
            newVersion.MajorVersion = majorVersion;
            newVersion.MinorVersion = minorVersion + 1;
            newVersion.Predecessors ??= new List<string>();
            newVersion.Predecessors.Add(template.Current.Id);
            newVersion.TemplateJson = command.TemplateJson;
            await _templateVersionRepository.SaveVersion(newVersion);

            // update template
            template.Name = template.Current.Name;
            template.AddPredecessor(template.Current.Id);
            template.Current = newVersion;
            
            HttpStatusCode updateResult = await _templateRepository.Update(template);
            if (updateResult == HttpStatusCode.OK)
            {
                await CreateTemplateIndexItem(template, author);
            }

            return updateResult;
        }
        
        private (T model, string errorMessage) Deserialise<T>(string json) where T : class
        {
            try
            {
                return (JsonConvert.DeserializeObject<T>(json), null);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to deserialize json : {ex.Message}");
                return (null, ex.Message);
            }
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
            newVersion.Name = $"{template.FundingStream.Id} {template.FundingPeriod.Id}"; // migrate old data
            newVersion.Description = command.Description;
            newVersion.Version = template.Current.Version + 1;
            newVersion.MinorVersion = template.Current.MinorVersion + 1;
            newVersion.MajorVersion = template.Current.MajorVersion;
            newVersion.Predecessors ??= new List<string>();
            newVersion.Predecessors.Add(template.Current.Id);
            if (!template.Current.TemplateJson.IsNullOrEmpty())
            {
                (SchemaJson templateContent, string error) = Deserialise<SchemaJson>(template.Current.TemplateJson);
                if (error != null)
                {
                    _logger.Error("Failed to deserialise json template: " + error);
                    return HttpStatusCode.BadRequest;
                }
                templateContent.FundingTemplate.FundingTemplateVersion = $"{newVersion.MajorVersion}.{newVersion.MinorVersion}";
                templateContent.FundingTemplate.FundingPeriod = Map(template.FundingPeriod);
                templateContent.FundingTemplate.FundingStream = Map(template.FundingStream);
                newVersion.TemplateJson = templateContent.AsJson();
            }
            var result = await _templateVersionRepository.SaveVersion(newVersion);
            if (!result.IsSuccess())
                return result;

            // update template
            template.Name = $"{template.FundingStream.Id} {template.FundingPeriod.Id}";
            template.AddPredecessor(template.Current.Id);
            template.Current = newVersion;

            HttpStatusCode updateResult = await _templateRepository.Update(template);
            if (updateResult == HttpStatusCode.OK)
            {
                await CreateTemplateIndexItem(template, author);
            }

            return updateResult;
        }

        private (ValidationFailure, TemplateJsonContentUpdateCommand) MapCommand(TemplateFundingLinesUpdateCommand command, Template template, TemplateStatus status, int majorVersion, int minorVersion)
        {
            (IEnumerable<SchemaJsonFundingLine> fundingLines, string errorMessage) = Deserialise<IEnumerable<SchemaJsonFundingLine>>(command.TemplateFundingLinesJson);
            if (!errorMessage.IsNullOrEmpty())
            {
                _logger.Error("Failed to deserialise json template: " + errorMessage);
                return (new ValidationFailure(nameof(command.TemplateFundingLinesJson), errorMessage), null);
            }
            var templateJson = new SchemaJson
            {
                Schema = "https://fundingschemas.blob.core.windows.net/schemas/funding-template-schema-1.1.json",
                SchemaVersion = "1.1",
                FundingTemplate = new SchemaJsonFundingStreamTemplate
                {
                    FundingLines = fundingLines,
                    FundingPeriod = Map(template.FundingPeriod),
                    FundingStream = Map(template.FundingStream),
                    FundingTemplateVersion = $"{majorVersion}.{minorVersion}"
                }
            };
            
            return (null, new TemplateJsonContentUpdateCommand
            {
                TemplateId = command.TemplateId,
                TemplateJson = templateJson.AsJson()
            });
        }

        private SchemaJsonFundingStream Map(FundingStream templateFundingStream)
        {
            return new SchemaJsonFundingStream
            {
                Code = templateFundingStream.Id,
                Name = templateFundingStream.Name
            };
        }

        private SchemaJsonFundingPeriod Map(FundingPeriod templateFundingPeriod)
        {
            return new SchemaJsonFundingPeriod
            {
                Name = templateFundingPeriod.Name,
                Period = templateFundingPeriod.Period,
                Type = (CalculateFunding.Common.TemplateMetadata.Schema11.Models.FundingPeriodType) 
                    Enum.Parse(typeof(CalculateFunding.Common.TemplateMetadata.Schema11.Models.FundingPeriodType), 
                        templateFundingPeriod.Type.ToString()),
                StartDate = templateFundingPeriod.StartDate,
                EndDate = templateFundingPeriod.EndDate
            };
        }

        private async Task<CommandResult> ValidateTemplateContent(string templateJson)
        {
            // template json validation
            FundingTemplateValidationResult validationResult = await _fundingTemplateValidationService.ValidateFundingTemplate(templateJson);

            if (!validationResult.IsValid)
            {
                return CommandResult.ValidationFail(validationResult);
            }

            // schema specific validation
            ITemplateMetadataGenerator templateMetadataGenerator = _templateMetadataResolver.GetService(validationResult.SchemaVersion);

            ValidationResult validationGeneratorResult = templateMetadataGenerator.Validate(templateJson);

            if (!validationGeneratorResult.IsValid)
            {
                return CommandResult.ValidationFail(validationGeneratorResult);
            }

            return null;
        }

        private async Task CreateTemplateIndexItem(Template template, Reference author)
        {
            var fundingPeriod = await _policyRepository.GetFundingPeriodById(template.Current.FundingPeriodId);
            var fundingStream = await _policyRepository.GetFundingStreamById(template.Current.FundingStreamId);

            TemplateIndex templateIndex = new TemplateIndex
            {
                Id = template.TemplateId,
                Name = template.Current.Name,
                FundingStreamId = template.Current.FundingStreamId,
                FundingStreamName = fundingStream.ShortName,
                FundingPeriodId = template.Current.FundingPeriodId,
                FundingPeriodName = fundingPeriod.Name,
                LastUpdatedAuthorId = author.Id,
                LastUpdatedAuthorName = author.Name,
                LastUpdatedDate = DateTimeOffset.Now,
                Version = template.Current.Version,
                CurrentMajorVersion = template.Current.MajorVersion,
                CurrentMinorVersion = template.Current.MinorVersion,
                PublishedMajorVersion = template.Released?.MajorVersion ?? 0,
                PublishedMinorVersion = template.Released?.MinorVersion ?? 0,
                HasReleasedVersion = template.Released != null ? "Yes" : "No"
            };

            await _searchRepository.Index(new List<TemplateIndex>
            {
                templateIndex
            });
        }
    }
}