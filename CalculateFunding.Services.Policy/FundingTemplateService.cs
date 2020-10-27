using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Policy.TemplateBuilder;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Services.Policy.Models;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace CalculateFunding.Services.Policy
{
    public struct FundingTemplateVersionBlobName
    {
        public FundingTemplateVersionBlobName(string fundingStreamId,
            string fundingPeriodId,
            string templateVersion)
        {
            Value = $"{fundingStreamId}/{fundingPeriodId}/{templateVersion}.json";
        }

        public string Value { get; private set; }

        public static implicit operator string(FundingTemplateVersionBlobName blobName) => blobName.Value;
    }

    public class FundingTemplateService : IFundingTemplateService, IHealthChecker
    {
        private readonly ILogger _logger;
        private readonly IFundingTemplateRepository _fundingTemplateRepository;
        private readonly Polly.AsyncPolicy _fundingTemplateRepositoryPolicy;
        private readonly IFundingTemplateValidationService _fundingTemplateValidationService;
        private readonly ICacheProvider _cacheProvider;
        private readonly Polly.AsyncPolicy _cacheProviderPolicy;
        private readonly ITemplateMetadataResolver _templateMetadataResolver;
        private readonly ITemplateBuilderService _templateBuilderService;
        private readonly IMapper _mapper;

        public FundingTemplateService(
            ILogger logger,
            IFundingTemplateRepository fundingTemplateRepository,
            IPolicyResiliencePolicies policyResiliencePolicies,
            IFundingTemplateValidationService fundingTemplateValidationService,
            ICacheProvider cacheProvider,
            ITemplateMetadataResolver templateMetadataResolver,
            ITemplateBuilderService templateBuilderService,
            IMapper mapper)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(fundingTemplateRepository, nameof(fundingTemplateRepository));
            Guard.ArgumentNotNull(policyResiliencePolicies?.CacheProvider, nameof(policyResiliencePolicies.CacheProvider));
            Guard.ArgumentNotNull(policyResiliencePolicies?.FundingTemplateRepository, nameof(policyResiliencePolicies.FundingTemplateRepository));
            Guard.ArgumentNotNull(fundingTemplateValidationService, nameof(fundingTemplateValidationService));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(templateMetadataResolver, nameof(templateMetadataResolver));
            Guard.ArgumentNotNull(templateBuilderService, nameof(templateBuilderService));
            Guard.ArgumentNotNull(mapper, nameof(mapper));

            _logger = logger;
            _fundingTemplateRepository = fundingTemplateRepository;
            _fundingTemplateRepositoryPolicy = policyResiliencePolicies.FundingTemplateRepository;
            _fundingTemplateValidationService = fundingTemplateValidationService;
            _cacheProvider = cacheProvider;
            _cacheProviderPolicy = policyResiliencePolicies.CacheProvider;
            _templateMetadataResolver = templateMetadataResolver;
            _templateBuilderService = templateBuilderService;
            _mapper = mapper;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) fundingTemplateRepoHealth = await ((BlobClient)_fundingTemplateRepository).IsHealthOk();
            ServiceHealth fundingTemplateValidationServiceHealth = await ((IHealthChecker)_fundingTemplateValidationService).IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(FundingSchemaService)
            };

            health.Dependencies.Add(new DependencyHealth { HealthOk = fundingTemplateRepoHealth.Ok, DependencyName = fundingTemplateRepoHealth.GetType().GetFriendlyName(), Message = fundingTemplateRepoHealth.Message });
            health.Dependencies.AddRange(fundingTemplateValidationServiceHealth.Dependencies);

            return health;
        }

        public async Task<IActionResult> SaveFundingTemplate(string actionName, string controllerName, string template, string fundingStreamId, string fundingPeriodId, string templateVersion)
        {
            Guard.IsNullOrWhiteSpace(actionName, nameof(actionName));
            Guard.IsNullOrWhiteSpace(controllerName, nameof(controllerName));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(templateVersion, nameof(templateVersion));

            //Already checked for null above when getting body
            if (template.Trim() == string.Empty)
            {
                return new BadRequestObjectResult("Null or empty funding template was provided.");
            }

            FundingTemplateValidationResult validationResult = await _fundingTemplateValidationService.ValidateFundingTemplate(template, fundingStreamId, fundingPeriodId, templateVersion);

            if (!validationResult.IsValid)
            {
                return validationResult.AsBadRequest();
            }

            ITemplateMetadataGenerator templateMetadataGenerator = _templateMetadataResolver.GetService(validationResult.SchemaVersion);

            ValidationResult validationGeneratorResult = templateMetadataGenerator.Validate(template);

            if (!validationGeneratorResult.IsValid)
            {
                return validationGeneratorResult.PopulateModelState();
            }

            string blobName = GetBlobNameFor(validationResult.FundingStreamId, validationResult.FundingPeriodId, validationResult.TemplateVersion);

            try
            {
                byte[] templateFileBytes = Encoding.UTF8.GetBytes(template);

                await SaveFundingTemplateVersion(blobName, templateFileBytes);

                string cacheKey = $"{CacheKeys.FundingTemplatePrefix}{validationResult.FundingStreamId}-{validationResult.FundingPeriodId}-{validationResult.TemplateVersion}".ToLowerInvariant();
                await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.RemoveAsync<string>(cacheKey));
                await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.RemoveAsync<FundingTemplateContents>($"{CacheKeys.FundingTemplateContents}{validationResult.FundingStreamId}:{validationResult.FundingPeriodId}:{validationResult.TemplateVersion}".ToLowerInvariant()));
                await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.RemoveAsync<TemplateMetadataContents>($"{CacheKeys.FundingTemplateContentMetadata}{validationResult.FundingStreamId}:{validationResult.FundingPeriodId}:{validationResult.TemplateVersion}".ToLowerInvariant()));
                await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.RemoveAsync<TemplateMetadataDistinctContents>($"{CacheKeys.FundingTemplateContentMetadataDistinct}{validationResult.FundingStreamId}:{validationResult.FundingPeriodId}:{validationResult.TemplateVersion}".ToLowerInvariant()));

                return new CreatedAtActionResult(actionName, controllerName, new { fundingStreamId = validationResult.FundingStreamId, fundingPeriodId = validationResult.FundingPeriodId, templateVersion = validationResult.TemplateVersion }, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to save funding template '{blobName}' to blob storage");

                return new InternalServerErrorResult("Error occurred uploading funding template");
            }
        }

        public async Task<IActionResult> GetFundingTemplateSourceFile(string fundingStreamId, string fundingPeriodId, string templateVersion)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(templateVersion, nameof(templateVersion));

            string cacheKey = $"{CacheKeys.FundingTemplatePrefix}{fundingStreamId}-{fundingPeriodId}-{templateVersion}";

            string template = await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.GetAsync<string>(cacheKey));

            if (!string.IsNullOrWhiteSpace(template))
            {
                return new OkObjectResult(template);
            }

            string blobName = GetBlobNameFor(fundingStreamId, fundingPeriodId, templateVersion);

            try
            {
                bool versionExists = await CheckIfFundingTemplateVersionExists(blobName);

                if (!versionExists)
                {
                    return new NotFoundResult();
                }

                template = await GetFundingTemplateVersion(blobName);

                if (string.IsNullOrWhiteSpace(template))
                {
                    _logger.Error($"Empty template returned from blob storage for blob name '{blobName}'");
                    return new InternalServerErrorResult($"Failed to retreive blob contents for funding stream id '{fundingStreamId}', funding period id '{fundingPeriodId}' and funding template version '{templateVersion}'");
                }

                await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.SetAsync(cacheKey, template));

                return new OkObjectResult(template);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to fetch funding template '{blobName}' from blob storage");

                return new InternalServerErrorResult($"Error occurred fetching funding template for funding stream id '{fundingStreamId}', funding period id '{fundingPeriodId}' and version '{templateVersion}'");
            }
        }

        private static string GetBlobNameFor(string fundingStreamId, string fundingPeriodId, string templateVersion)
            => new FundingTemplateVersionBlobName(fundingStreamId, fundingPeriodId, templateVersion);

        private async Task SaveFundingTemplateVersion(string blobName, byte[] templateBytes)
        {
            try
            {
                await _fundingTemplateRepositoryPolicy.ExecuteAsync(() => _fundingTemplateRepository.SaveFundingTemplateVersion(blobName, templateBytes));
            }
            catch (Exception ex)
            {
                throw new NonRetriableException($"Failed to save funding template version: '{blobName}'", ex);
            }
        }

        private async Task<bool> CheckIfFundingTemplateVersionExists(string blobName)
        {
            try
            {
                return await _fundingTemplateRepositoryPolicy.ExecuteAsync(() => _fundingTemplateRepository.TemplateVersionExists(blobName));
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to check if funding template version: '{blobName}' exists");

                throw new NonRetriableException($"Failed to check if funding template version: '{blobName}' exists", ex);
            }
        }

        private async Task<string> GetFundingTemplateVersion(string blobName)
        {
            try
            {
                return await _fundingTemplateRepositoryPolicy.ExecuteAsync(() => _fundingTemplateRepository.GetFundingTemplateVersion(blobName));
            }
            catch (Exception ex)
            {
                throw new NonRetriableException($"Failed to get funding template version: '{blobName}' from blob storage", ex);
            }
        }

        private async Task<IEnumerable<PublishedFundingTemplate>> SearchTemplates(string blobNamePrefix)
        {
            try
            {
                return await _fundingTemplateRepositoryPolicy.ExecuteAsync(() => _fundingTemplateRepository.SearchTemplates(blobNamePrefix));
            }
            catch (Exception ex)
            {
                throw new NonRetriableException($"Failed to search for funding template: '{blobNamePrefix}' from blob storage", ex);
            }
        }

        public async Task<IActionResult> GetFundingTemplateContents(string fundingStreamId, string fundingPeriodId, string templateVersion)
        {
            IActionResult getFundingTemplateMetadataResult = await GetFundingTemplateContentMetadata(fundingStreamId, fundingPeriodId, templateVersion);
            if (!(getFundingTemplateMetadataResult is OkObjectResult))
            {
                return getFundingTemplateMetadataResult;
            }
            TemplateMetadataContents fundingTemplateContents = (getFundingTemplateMetadataResult as OkObjectResult).Value as TemplateMetadataContents;

            return new OkObjectResult(fundingTemplateContents);
        }

        public async Task<IActionResult> GetFundingTemplate(string fundingStreamId, string fundingPeriodId, string templateVersion)
        {
            IActionResult getFundingTemplateContentResult = await GetFundingTemplateContentsInner(fundingStreamId, fundingPeriodId, templateVersion);
            if (!(getFundingTemplateContentResult is OkObjectResult))
            {
                return getFundingTemplateContentResult;
            }
            FundingTemplateContents fundingTemplateContents = (getFundingTemplateContentResult as OkObjectResult).Value as FundingTemplateContents;

            return new OkObjectResult(fundingTemplateContents);
        }

        public async Task<bool> TemplateExists(string fundingStreamId, string fundingPeriodId, string templateVersion)
        {
            return await CheckIfFundingTemplateVersionExists(GetBlobNameFor(fundingStreamId, fundingPeriodId, templateVersion));
        }

        public async Task<IActionResult> GetFundingTemplates(string fundingStreamId, string fundingPeriodId)
        {
            IEnumerable<PublishedFundingTemplate> publishFundingTemplates = await SearchTemplates($"{fundingStreamId}/{fundingPeriodId}/");
            IEnumerable<TemplateSummaryResponse> templateResponses = await _templateBuilderService.FindVersionsByFundingStreamAndPeriod(new FindTemplateVersionQuery()
            {
                FundingStreamId = fundingStreamId,
                FundingPeriodId = fundingPeriodId,
                Statuses = new List<TemplateStatus>() { TemplateStatus.Published }
            });

            foreach (var publishedFundingTemplate in publishFundingTemplates)
            {
                var versionParts = publishedFundingTemplate.TemplateVersion.Split(".");
                if (versionParts.Length >= 2)
                {
                    TemplateSummaryResponse templateResponse = templateResponses.FirstOrDefault(x => x.MajorVersion.ToString() == versionParts[0] && x.MinorVersion.ToString() == versionParts[1]);
                    if (templateResponse != null)
                    {
                        publishedFundingTemplate.PublishNote = templateResponse.Comments;
                        publishedFundingTemplate.AuthorId = templateResponse.AuthorId;
                        publishedFundingTemplate.AuthorName = templateResponse.AuthorName;
                        publishedFundingTemplate.SchemaVersion = templateResponse.SchemaVersion;
                    }
                }
            }

            return publishFundingTemplates.Any() ? (IActionResult)new OkObjectResult(publishFundingTemplates) : new NotFoundResult();
        }

        public async Task<IActionResult> GetDistinctFundingTemplateMetadataFundingLinesContents(string fundingStreamId, string fundingPeriodId, string templateVersion)
        {
            IActionResult templateMetadataDistinctContentsResult = await GetDistinctFundingTemplateMetadataContents(fundingStreamId, fundingPeriodId, templateVersion);
            if (!(templateMetadataDistinctContentsResult is OkObjectResult))
            {
                return templateMetadataDistinctContentsResult;
            }

            TemplateMetadataDistinctContents templateMetadataDistinctContents = (templateMetadataDistinctContentsResult as OkObjectResult).Value as TemplateMetadataDistinctContents;

            TemplateMetadataDistinctFundingLinesContents templateMetadataDistinctFundingLinesContents = new TemplateMetadataDistinctFundingLinesContents()
            {
                FundingStreamId = templateMetadataDistinctContents.FundingStreamId,
                FundingPeriodId = templateMetadataDistinctContents.FundingPeriodId,
                TemplateVersion = templateMetadataDistinctContents.TemplateVersion,
                FundingLines = templateMetadataDistinctContents.FundingLines,
                SchemaVersion = templateMetadataDistinctContents.SchemaVersion
            };

            return new OkObjectResult(templateMetadataDistinctFundingLinesContents);
        }

        public async Task<IActionResult> GetDistinctFundingTemplateMetadataCalculationsContents(string fundingStreamId, string fundingPeriodId, string templateVersion)
        {
            IActionResult templateMetadataDistinctContentsResult = await GetDistinctFundingTemplateMetadataContents(fundingStreamId, fundingPeriodId, templateVersion);
            if (!(templateMetadataDistinctContentsResult is OkObjectResult))
            {
                return templateMetadataDistinctContentsResult;
            }

            TemplateMetadataDistinctContents templateMetadataDistinctContents = (templateMetadataDistinctContentsResult as OkObjectResult).Value as TemplateMetadataDistinctContents;

            TemplateMetadataDistinctCalculationsContents templateMetadataDistinctCalculationsContents = new TemplateMetadataDistinctCalculationsContents()
            {
                FundingStreamId = templateMetadataDistinctContents.FundingStreamId,
                FundingPeriodId = templateMetadataDistinctContents.FundingPeriodId,
                TemplateVersion = templateMetadataDistinctContents.TemplateVersion,
                Calculations = templateMetadataDistinctContents.Calculations,
                SchemaVersion = templateMetadataDistinctContents.SchemaVersion
            };

            return new OkObjectResult(templateMetadataDistinctCalculationsContents);
        }

        public async Task<IActionResult> GetDistinctFundingTemplateMetadataContents(string fundingStreamId, string fundingPeriodId, string templateVersion)
        {
            string cacheKey = $"{CacheKeys.FundingTemplateContentMetadataDistinct}{fundingStreamId}:{fundingPeriodId}:{templateVersion}".ToLowerInvariant();
            TemplateMetadataDistinctContents templateMetadataDistinctContents = await _cacheProvider.GetAsync<TemplateMetadataDistinctContents>(cacheKey);

            if (templateMetadataDistinctContents == null)
            {
                IActionResult fundingTemplateContentMetadataResult = await GetFundingTemplateContentMetadata(fundingStreamId, fundingPeriodId, templateVersion);
                if (!(fundingTemplateContentMetadataResult is OkObjectResult))
                {
                    return fundingTemplateContentMetadataResult;
                }

                TemplateMetadataContents templateMetadataContents = (fundingTemplateContentMetadataResult as OkObjectResult).Value as TemplateMetadataContents;
                IEnumerable<TemplateMetadataCalculation> calculations = null;
                IEnumerable<TemplateMetadataFundingLine> fundingLines = null;
                if (templateMetadataContents.RootFundingLines != null)
                {
                    IEnumerable<FundingLine> flattenedFundingLines = templateMetadataContents.RootFundingLines.Flatten(x => x.FundingLines);
                    IEnumerable<Calculation> flatternedCalculations = flattenedFundingLines.SelectMany(fl => fl.Calculations.Flatten(cal => cal.Calculations));

                    IEnumerable<FundingLine> uniqueFundingLines = flattenedFundingLines.DistinctBy(f => f.TemplateLineId);
                    IEnumerable<Calculation> uniqueCalculations = flatternedCalculations.DistinctBy(c => c.TemplateCalculationId);

                    fundingLines = _mapper.Map<IEnumerable<TemplateMetadataFundingLine>>(uniqueFundingLines);
                    calculations = _mapper.Map<IEnumerable<TemplateMetadataCalculation>>(uniqueCalculations);
                }

                templateMetadataDistinctContents = new TemplateMetadataDistinctContents()
                {
                    FundingStreamId = templateMetadataContents.FundingStreamId,
                    FundingPeriodId = templateMetadataContents.FundingPeriodId,
                    TemplateVersion = templateMetadataContents.TemplateVersion,
                    SchemaVersion = templateMetadataContents.SchemaVersion,
                    FundingLines = fundingLines,
                    Calculations = calculations
                };

                // We need to cache as long as the data has not been changed
                await _cacheProvider.SetAsync(cacheKey, templateMetadataDistinctContents, TimeSpan.FromDays(365), true);
            }

            return new OkObjectResult(templateMetadataDistinctContents);
        }

        private async Task<IActionResult> GetFundingTemplateContentMetadata(string fundingStreamId, string fundingPeriodId, string templateVersion)
        {
            TemplateMetadataContents fundingTemplateContentMetadata = await _cacheProvider.GetAsync<TemplateMetadataContents>($"{CacheKeys.FundingTemplateContentMetadata}{fundingStreamId}:{fundingPeriodId}:{templateVersion}");
            if (fundingTemplateContentMetadata == null)
            {
                IActionResult fundingTemplateContentSourceFileResult = await GetFundingTemplateSourceFile(fundingStreamId, fundingPeriodId, templateVersion);

                if (!(fundingTemplateContentSourceFileResult is OkObjectResult))
                {
                    return fundingTemplateContentSourceFileResult;
                }
                string fundingTemplateContentSourceFile = (fundingTemplateContentSourceFileResult as OkObjectResult).Value as string;

                IActionResult fundingTemplateValidationResult = GetFundingTemplateSchemaVersion(fundingTemplateContentSourceFile);
                if (!(fundingTemplateValidationResult is OkObjectResult))
                {
                    return fundingTemplateValidationResult;
                }
                string fundingTemplateSchemaVersion = (fundingTemplateValidationResult as OkObjectResult).Value as string;

                bool templateMetadataGeneratorRetrieved = _templateMetadataResolver.TryGetService(fundingTemplateSchemaVersion, out ITemplateMetadataGenerator templateMetadataGenerator);
                if (!templateMetadataGeneratorRetrieved)
                {
                    string message = $"Template metadata generator with given schema {fundingTemplateSchemaVersion} could not be retrieved.";
                    _logger.Error(message);

                    return new PreconditionFailedResult(message);
                }

                string blobName = GetBlobNameFor(fundingStreamId, fundingPeriodId, templateVersion);

                fundingTemplateContentMetadata = templateMetadataGenerator.GetMetadata(fundingTemplateContentSourceFile);

                fundingTemplateContentMetadata.FundingStreamId = fundingStreamId;
                fundingTemplateContentMetadata.FundingPeriodId = fundingPeriodId;
                fundingTemplateContentMetadata.TemplateVersion = templateVersion;
                fundingTemplateContentMetadata.LastModified = await _fundingTemplateRepositoryPolicy.ExecuteAsync(() => _fundingTemplateRepository.GetLastModifiedDate(blobName));

                await _cacheProvider.SetAsync($"{CacheKeys.FundingTemplateContentMetadata}{fundingStreamId}:{fundingPeriodId}:{templateVersion}", fundingTemplateContentMetadata);
            }

            return new OkObjectResult(fundingTemplateContentMetadata);
        }

        private async Task<IActionResult> GetFundingTemplateContentsInner(string fundingStreamId, string fundingPeriodId, string templateVersion)
        {
            IActionResult fundingTemplateContentSourceFileResult = await GetFundingTemplateSourceFile(fundingStreamId, fundingPeriodId, templateVersion);
            if (!(fundingTemplateContentSourceFileResult is OkObjectResult))
            {
                return fundingTemplateContentSourceFileResult;
            }
            string fundingTemplateContentSourceFile = (fundingTemplateContentSourceFileResult as OkObjectResult).Value as string;

            IActionResult fundingTemplateContentMetadataResult = await GetFundingTemplateContentMetadata(fundingStreamId, fundingPeriodId, templateVersion);
            if (!(fundingTemplateContentMetadataResult is OkObjectResult))
            {
                return fundingTemplateContentMetadataResult;
            }
            TemplateMetadataContents fundingTemplateContentMetadata = (fundingTemplateContentMetadataResult as OkObjectResult).Value as TemplateMetadataContents;

            FundingTemplateContents fundingTemplateContents = new FundingTemplateContents
            {
                TemplateFileContents = fundingTemplateContentSourceFile,
                Metadata = fundingTemplateContentMetadata
            };

            return new OkObjectResult(fundingTemplateContents);
        }

        private IActionResult GetFundingTemplateSchemaVersion(string fundingTemplateContent)
        {
            Guard.IsNullOrWhiteSpace(fundingTemplateContent, nameof(fundingTemplateContent));

            JObject parsedFundingTemplate;

            try
            {
                parsedFundingTemplate = JObject.Parse(fundingTemplateContent);
            }
            catch (JsonReaderException jre)
            {
                return new PreconditionFailedResult(jre.Message);
            }

            if (parsedFundingTemplate["schemaVersion"] == null ||
                string.IsNullOrWhiteSpace(parsedFundingTemplate["schemaVersion"].Value<string>()))
            {
                return new PreconditionFailedResult("Missing schema version from funding template.");
            }

            return new OkObjectResult(parsedFundingTemplate["schemaVersion"].Value<string>());
        }
    }
}
