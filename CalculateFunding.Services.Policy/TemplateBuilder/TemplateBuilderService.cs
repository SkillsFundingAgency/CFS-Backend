using System;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy.TemplateBuilder;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Services.Policy.Models;
using FluentValidation;
using FluentValidation.Results;
using Serilog;

namespace CalculateFunding.Services.Policy.TemplateBuilder
{
    public class TemplateBuilderService : ITemplateBuilderService, IHealthChecker
    {
        private readonly IValidator<TemplateCreateCommand> _templateCreateCommandValidator;
        private readonly IVersionRepository<TemplateVersion> _templateVersionRepository;
        private readonly ILogger _logger;
        private readonly ITemplateRepository _templateRepository;

        public TemplateBuilderService(
            IValidator<TemplateCreateCommand> templateCreateCommandValidator,
            IVersionRepository<TemplateVersion> templateVersionRepository,
            ITemplateRepository templateRepository,
            ILogger logger)
        {
            _templateCreateCommandValidator = templateCreateCommandValidator;
            _templateVersionRepository = templateVersionRepository;
            _templateRepository = templateRepository;
            _logger = logger;
        }
        
        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth repoHealth = await ((IHealthChecker)_templateRepository).IsHealthOk();

            ServiceHealth health = new ServiceHealth
            {
                Name = GetType().Name
            };
            health.Dependencies.AddRange(repoHealth.Dependencies);

            return health;
        }

        public async Task<CreateTemplateResponse> CreateTemplate(
            TemplateCreateCommand command,
            Reference author)
        {
            Guard.ArgumentNotNull(command, nameof(command));
            Guard.ArgumentNotNull(author, nameof(author));

            ValidationResult validationResult = await _templateCreateCommandValidator.ValidateAsync(command);

            if (!validationResult.IsValid)
            {
                return new CreateTemplateResponse
                {
                    ValidationResult = validationResult
                };
            }

            try
            {
                if (await _templateRepository.IsTemplateNameInUse(command.Name))
                {
                    string validationErrorMessage = $"Template name [{command.Name}] already in use";
                    _logger.Error(validationErrorMessage);
                    validationResult.Errors.Add(new ValidationFailure(nameof(command.Name), validationErrorMessage));
                    return new CreateTemplateResponse
                    {
                        ErrorMessage = validationErrorMessage,
                        ValidationResult = validationResult
                    };
                }

                Template template = new Template
                {
                    Current = new TemplateVersion
                    {
                        FundingStreamId = command.FundingStreamId,
                        Name = command.Name,
                        Description = command.Description,
                        Version = 1,
                        PublishStatus = PublishStatus.Draft,
                        SchemaVersion = command.SchemaVersion,
                        Author = author,
                        Date = DateTimeOffset.Now.ToLocalTime()
                    }
                };

                HttpStatusCode result = await _templateRepository.CreateDraft(template);

                if (result.IsSuccess())
                {
                    await _templateVersionRepository.SaveVersion(template.Current);

                    return new CreateTemplateResponse
                    {
                        Succeeded = true,
                        TemplateId = template.Id
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
    }
}