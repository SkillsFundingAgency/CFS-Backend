using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public async Task<ActionResult<string>> GetFundingTemplateSourceFile(string fundingStreamId, string fundingPeriodId, string templateVersion)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(templateVersion, nameof(templateVersion));

            string cacheKey = $"{CacheKeys.FundingTemplatePrefix}{fundingStreamId}-{fundingPeriodId}-{templateVersion}";

            string template = await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.GetAsync<string>(cacheKey));

            if (!string.IsNullOrWhiteSpace(template))
            {
                return template;
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

                return template;
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

        public async Task<ActionResult<TemplateMetadataContents>> GetFundingTemplateContents(string fundingStreamId, string fundingPeriodId, string templateVersion)
        {
            return await GetFundingTemplateContentMetadata(fundingStreamId, fundingPeriodId, templateVersion);
        }

        public async Task<ActionResult<FundingTemplateContents>> GetFundingTemplate(string fundingStreamId, string fundingPeriodId, string templateVersion)
        {
            return await GetFundingTemplateContentsInner(fundingStreamId, fundingPeriodId, templateVersion);
        }

        public async Task<bool> TemplateExists(string fundingStreamId, string fundingPeriodId, string templateVersion)
        {
            return await CheckIfFundingTemplateVersionExists(GetBlobNameFor(fundingStreamId, fundingPeriodId, templateVersion));
        }

        public async Task<ActionResult<IEnumerable<PublishedFundingTemplate>>> GetFundingTemplates(string fundingStreamId, string fundingPeriodId)
        {
            IEnumerable<PublishedFundingTemplate> publishFundingTemplates = await SearchTemplates($"{fundingStreamId}/{fundingPeriodId}/");
            IEnumerable<TemplateSummaryResponse> templateResponses = await _templateBuilderService.FindVersionsByFundingStreamAndPeriod(new FindTemplateVersionQuery()
            {
                FundingStreamId = fundingStreamId,
                FundingPeriodId = fundingPeriodId,
                Statuses = new List<TemplateStatus>() { TemplateStatus.Published }
            });

            foreach (PublishedFundingTemplate publishedFundingTemplate in publishFundingTemplates)
            {
                string[] versionParts = publishedFundingTemplate.TemplateVersion.Split(".");
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

            if (!publishFundingTemplates.Any()) { return new NotFoundResult(); }

            return publishFundingTemplates.ToList();
        }

        public async Task<ActionResult<TemplateMetadataDistinctFundingLinesContents>> GetDistinctFundingTemplateMetadataFundingLinesContents(string fundingStreamId, string fundingPeriodId, string templateVersion)
        {
            ActionResult<TemplateMetadataDistinctContents> templateMetadataDistinctContentsResult = await GetDistinctFundingTemplateMetadataContents(fundingStreamId, fundingPeriodId, templateVersion);
            if (templateMetadataDistinctContentsResult == null || templateMetadataDistinctContentsResult.Value == null)
            {
                return templateMetadataDistinctContentsResult.Result;
            }

            TemplateMetadataDistinctContents templateMetadataDistinctContents = templateMetadataDistinctContentsResult.Value;

            return new TemplateMetadataDistinctFundingLinesContents()
            {
                FundingStreamId = templateMetadataDistinctContents.FundingStreamId,
                FundingPeriodId = templateMetadataDistinctContents.FundingPeriodId,
                TemplateVersion = templateMetadataDistinctContents.TemplateVersion,
                FundingLines = templateMetadataDistinctContents.FundingLines,
                SchemaVersion = templateMetadataDistinctContents.SchemaVersion
            };
        }

        public async Task<ActionResult<TemplateMetadataDistinctCalculationsContents>> GetDistinctFundingTemplateMetadataCalculationsContents(string fundingStreamId, string fundingPeriodId, string templateVersion)
        {
            ActionResult<TemplateMetadataDistinctContents> templateMetadataDistinctContentsResult = await GetDistinctFundingTemplateMetadataContents(fundingStreamId, fundingPeriodId, templateVersion);
            if (templateMetadataDistinctContentsResult == null || templateMetadataDistinctContentsResult.Value == null)
            {
                return templateMetadataDistinctContentsResult.Result;
            }

            TemplateMetadataDistinctContents templateMetadataDistinctContents = templateMetadataDistinctContentsResult.Value;

            return new TemplateMetadataDistinctCalculationsContents()
            {
                FundingStreamId = templateMetadataDistinctContents.FundingStreamId,
                FundingPeriodId = templateMetadataDistinctContents.FundingPeriodId,
                TemplateVersion = templateMetadataDistinctContents.TemplateVersion,
                Calculations = templateMetadataDistinctContents.Calculations,
                SchemaVersion = templateMetadataDistinctContents.SchemaVersion
            };
        }

        public async Task<ActionResult<TemplateMetadataDistinctContents>> GetDistinctFundingTemplateMetadataContents(string fundingStreamId, string fundingPeriodId, string templateVersion)
        {
            string cacheKey = $"{CacheKeys.FundingTemplateContentMetadataDistinct}{fundingStreamId}:{fundingPeriodId}:{templateVersion}".ToLowerInvariant();
            TemplateMetadataDistinctContents templateMetadataDistinctContents = await _cacheProvider.GetAsync<TemplateMetadataDistinctContents>(cacheKey);

            if (templateMetadataDistinctContents == null)
            {
                ActionResult<TemplateMetadataContents> fundingTemplateContentMetadataResult = await GetFundingTemplateContentMetadata(fundingStreamId, fundingPeriodId, templateVersion);
                if (fundingTemplateContentMetadataResult.Result != null)
                {
                    return fundingTemplateContentMetadataResult.Result;
                }

                TemplateMetadataContents templateMetadataContents = fundingTemplateContentMetadataResult.Value;
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

            return templateMetadataDistinctContents;
        }

        public async Task<ActionResult<TemplateMetadataFundingLineCashCalculationsContents>> GetCashCalcsForTemplateVersion(string fundingStreamId, string fundingPeriodId, string templateVersion)
        {
            ActionResult<TemplateMetadataContents> templateMetadata = await GetFundingTemplateContentMetadata(fundingStreamId, fundingPeriodId, templateVersion);

            if (templateMetadata.Result != null)
            {
                return templateMetadata.Result;
            }

            TemplateMetadataContents templateMetadataContents = templateMetadata.Value;

            IEnumerable<FundingLine> flattenedFundingLines = templateMetadataContents.RootFundingLines.Flatten(x => x.FundingLines);
            IEnumerable<Calculation> flatternedCalculations = flattenedFundingLines.SelectMany(fl => fl.Calculations.Flatten(cal => cal.Calculations));

            IEnumerable<FundingLine> uniqueFundingLines = flattenedFundingLines.DistinctBy(f => f.TemplateLineId);

            IOrderedEnumerable<FundingLine> paymentFundingLine = uniqueFundingLines.Where(f => f.Type == Common.TemplateMetadata.Enums.FundingLineType.Payment).OrderBy(f => f.FundingLineCode);

            Dictionary<string, IEnumerable<TemplateMetadataCalculation>> cashCalculations = new Dictionary<string, IEnumerable<TemplateMetadataCalculation>>();

            List<TemplateMetadataFundingLine> fundingLineResults = new List<TemplateMetadataFundingLine>(paymentFundingLine.Count());
            foreach (FundingLine fundingLine in paymentFundingLine)
            {
                fundingLineResults.Add(new TemplateMetadataFundingLine()
                {
                    FundingLineCode = fundingLine.FundingLineCode,
                    Name = fundingLine.Name,
                    TemplateLineId = fundingLine.TemplateLineId,
                    Type = Common.TemplateMetadata.Enums.FundingLineType.Payment,
                });

                List<TemplateMetadataCalculation> cashCalculationsForFundingLines = FindCashCalculations(fundingLine);

                cashCalculations.Add(fundingLine.FundingLineCode, cashCalculationsForFundingLines.OrderBy(_ => _.Name));
            }

            return new TemplateMetadataFundingLineCashCalculationsContents()
            {
                FundingPeriodId = fundingPeriodId,
                FundingStreamId = fundingStreamId,
                SchemaVersion = templateMetadata.Value.SchemaVersion,
                TemplateVersion = templateMetadata.Value.TemplateVersion,
                FundingLines = fundingLineResults,
                CashCalculations = cashCalculations,
            };
        }

        private List<TemplateMetadataCalculation> FindCashCalculations(FundingLine fundingLine)
        {
            List<TemplateMetadataCalculation> calculations = new List<TemplateMetadataCalculation>();

            List<TemplateMetadataCalculation> childCalcCashCalculations = FindCashCalculations(fundingLine.Calculations);
            if (childCalcCashCalculations.Count > 0)
            {
                calculations.AddRange(childCalcCashCalculations);
            }

            foreach (FundingLine childFundingLine in fundingLine.FundingLines)
            {
                List<TemplateMetadataCalculation> fundingLineCalculations = FindCashCalculations(childFundingLine);
                if (fundingLineCalculations.Count > 0)
                {
                    calculations.AddRange(fundingLineCalculations);
                }
            }

            return calculations;
        }

        private List<TemplateMetadataCalculation> FindCashCalculations(IEnumerable<Calculation> calculations)
        {
            List<TemplateMetadataCalculation> calculationsResult = new List<TemplateMetadataCalculation>();

            if (calculations != null)
            {
                foreach (Calculation calculation in calculations)
                {
                    if (calculation.Type == Common.TemplateMetadata.Enums.CalculationType.Cash)
                    {
                        calculationsResult.Add(_mapper.Map<TemplateMetadataCalculation>(calculation));
                    }
                    else
                    {
                        List<TemplateMetadataCalculation> childCalculations = FindCashCalculations(calculation.Calculations);
                        if (childCalculations.Count > 0)
                        {
                            calculationsResult.AddRange(childCalculations);
                        }
                    }
                }
            }

            return calculationsResult;
        }

        private async Task<ActionResult<TemplateMetadataContents>> GetFundingTemplateContentMetadata(string fundingStreamId, string fundingPeriodId, string templateVersion)
        {
            TemplateMetadataContents fundingTemplateContentMetadata = await _cacheProvider.GetAsync<TemplateMetadataContents>($"{CacheKeys.FundingTemplateContentMetadata}{fundingStreamId}:{fundingPeriodId}:{templateVersion}");
            if (fundingTemplateContentMetadata == null)
            {
                ActionResult<string> fundingTemplateContentSourceFileResult = await GetFundingTemplateSourceFile(fundingStreamId, fundingPeriodId, templateVersion);

                if (fundingTemplateContentSourceFileResult.Result != null)
                {
                    return fundingTemplateContentSourceFileResult.Result;
                }

                string fundingTemplateContentSourceFile = fundingTemplateContentSourceFileResult.Value;

                ActionResult<string> fundingTemplateValidationResult = GetFundingTemplateSchemaVersion(fundingTemplateContentSourceFile);
                if (fundingTemplateValidationResult.Result != null)
                {
                    return fundingTemplateValidationResult.Result;
                }

                string fundingTemplateSchemaVersion = fundingTemplateValidationResult.Value;

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

            return fundingTemplateContentMetadata;
        }

        private async Task<ActionResult<FundingTemplateContents>> GetFundingTemplateContentsInner(string fundingStreamId, string fundingPeriodId, string templateVersion)
        {
            ActionResult<string> fundingTemplateContentSourceFileResult = await GetFundingTemplateSourceFile(fundingStreamId, fundingPeriodId, templateVersion);
            if (fundingTemplateContentSourceFileResult.Result != null)
            {
                return fundingTemplateContentSourceFileResult.Result;
            }
            string fundingTemplateContentSourceFile = fundingTemplateContentSourceFileResult.Value;

            ActionResult<TemplateMetadataContents> fundingTemplateContentMetadataResult = await GetFundingTemplateContentMetadata(fundingStreamId, fundingPeriodId, templateVersion);
            if (fundingTemplateContentMetadataResult.Result != null)
            {
                return fundingTemplateContentMetadataResult.Result;
            }
            TemplateMetadataContents fundingTemplateContentMetadata = fundingTemplateContentMetadataResult.Value;

            return new FundingTemplateContents
            {
                TemplateFileContents = fundingTemplateContentSourceFile,
                Metadata = fundingTemplateContentMetadata
            };
        }

        private ActionResult<string> GetFundingTemplateSchemaVersion(string fundingTemplateContent)
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

            return parsedFundingTemplate["schemaVersion"].Value<string>();
        }
    }
}
