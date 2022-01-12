using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.SqlExport;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalcsApiCalculation = CalculateFunding.Common.ApiClient.Calcs.Models.Calculation;

namespace CalculateFunding.Services.Results.SqlExport
{
    public class SqlImportContextBuilder : ISqlImportContextBuilder
    {
        private readonly ICosmosRepository _cosmos;
        private readonly ICalculationsApiClient _calculationsApiClient;
        private readonly IJobsApiClient _jobsApiClient;
        private readonly IPoliciesApiClient _policies;
        private readonly ITemplateMetadataResolver _templateMetadataResolver;
        private readonly ISpecificationsApiClient _specifications;
        private readonly ISqlNameGenerator _sqlNameGenerator;
        private readonly AsyncPolicy _specificationResilience;
        private readonly AsyncPolicy _policiesResilience;
        private readonly AsyncPolicy _calcsResilience;
        private readonly AsyncPolicy _jobsResilience;

        public SqlImportContextBuilder(ICosmosRepository cosmos,
            IPoliciesApiClient policies,
            ITemplateMetadataResolver templateMetadataResolver,
            ISpecificationsApiClient specifications,
            IResultsResiliencePolicies resiliencePolicies,
            ISqlNameGenerator sqlNameGenerator,
            ICalculationsApiClient calculationsApiClient,
            IJobsApiClient jobsApiClient)
        {
            Guard.ArgumentNotNull(cosmos, nameof(cosmos));
            Guard.ArgumentNotNull(policies, nameof(policies));
            Guard.ArgumentNotNull(templateMetadataResolver, nameof(templateMetadataResolver));
            Guard.ArgumentNotNull(specifications, nameof(specifications));
            Guard.ArgumentNotNull(resiliencePolicies?.SpecificationsApiClient, nameof(resiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.PoliciesApiClient, nameof(resiliencePolicies.PoliciesApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.CalculationsApiClient, nameof(resiliencePolicies.CalculationsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.JobsApiClient, nameof(resiliencePolicies.JobsApiClient));
            Guard.ArgumentNotNull(sqlNameGenerator, nameof(sqlNameGenerator));
            Guard.ArgumentNotNull(calculationsApiClient, nameof(calculationsApiClient));
            Guard.ArgumentNotNull(jobsApiClient, nameof(jobsApiClient));

            _cosmos = cosmos;
            _policies = policies;
            _templateMetadataResolver = templateMetadataResolver;
            _specifications = specifications;
            _sqlNameGenerator = sqlNameGenerator;
            _specificationResilience = resiliencePolicies.SpecificationsApiClient;
            _policiesResilience = resiliencePolicies.PoliciesApiClient;
            _calcsResilience = resiliencePolicies.CalculationsApiClient;
            _calculationsApiClient = calculationsApiClient;
            _jobsApiClient = jobsApiClient;
            _jobsResilience = resiliencePolicies.JobsApiClient;
        }

        public async Task<ISqlImportContext> CreateImportContext(string specificationId)
        {
            ICosmosDbFeedIterator providerResultsFeed = GetProviderResultsFeed(specificationId);

            SpecificationSummary specificationSummary = await GetSpecificationSummary(specificationId);
            UniqueTemplateContents uniqueTemplateContents = await GetTemplateData(specificationSummary, specificationSummary.FundingStreams.FirstOrDefault().Id);
            IEnumerable<FundingLine> allFundingLines = uniqueTemplateContents.FundingLines;
            IEnumerable<Calculation> uniqueCalculations = uniqueTemplateContents.Calculations;

            IEnumerable<CalcsApiCalculation> calculations = await GetCalculations(specificationId);

            JobSummary jobSummary = await GetLatestCalcRunJobSummary(specificationId);

            ApiResponse<Common.ApiClient.Calcs.Models.CalculationIdentifier> specsIdentifierResponse =
            await _calcsResilience.ExecuteAsync(() => _calculationsApiClient.GenerateCalculationIdentifier(
                new Common.ApiClient.Calcs.Models.GenerateIdentifierModel { CalculationName = specificationSummary.Name }));

            Common.ApiClient.Calcs.Models.CalculationIdentifier specsIdentifier = specsIdentifierResponse.Content;

            if (specsIdentifier == null)
            {
                throw new NonRetriableException(
                    $"Did not generate identifiers for {specificationSummary.Name}. Unable to complete Qa Schema Generation");
            }

            string specificationTablePrefix = specsIdentifier.SourceCodeName;

            return new SqlImportContext
            {
                Documents = providerResultsFeed,
                CalculationRuns = new CalculationRunDataTableBuilder(specificationSummary, jobSummary, specificationTablePrefix),
                ProviderSummaries = new ProviderSummaryDataTableBuilder(specificationTablePrefix),
                PaymentFundingLines = new PaymentFundingLineDataTableBuilder(allFundingLines, _sqlNameGenerator, specificationTablePrefix),
                InformationFundingLines = new InformationFundingLineDataTableBuilder(allFundingLines, _sqlNameGenerator, specificationTablePrefix),
                TemplateCalculations = new TemplateCalculationsDataTableBuilder(calculations, _sqlNameGenerator, uniqueCalculations, specificationTablePrefix),
                AdditionalCalculations = new AdditionalCalculationsDataTableBuilder(calculations, _sqlNameGenerator, specificationTablePrefix),
            };
        }

        private async Task<JobSummary> GetLatestCalcRunJobSummary(string specificationId)
        {
            ApiResponse<JobSummary> latestCalcRunResponse =
                await _jobsResilience.ExecuteAsync(() => _jobsApiClient.GetLatestSuccessfulJobForSpecification(specificationId, JobConstants.DefinitionNames.CreateInstructAllocationJob));

            JobSummary jobSummary = latestCalcRunResponse.Content;

            if (jobSummary == null)
            {
                throw new NonRetriableException(
                    $"Did not locate calculations for {specificationId}. Unable to complete Qa Schema Generation");
            }

            return jobSummary;
        }

        private async Task<IEnumerable<CalcsApiCalculation>> GetCalculations(string specificationId)
        {
            ApiResponse<IEnumerable<CalcsApiCalculation>> calculationsResponse = 
                await _calcsResilience.ExecuteAsync(() => _calculationsApiClient.GetCalculationsForSpecification(specificationId));

            IEnumerable<CalcsApiCalculation> calculations = calculationsResponse.Content;

            if (calculations == null)
            {
                throw new NonRetriableException(
                    $"Did not locate calculations for {specificationId}. Unable to complete Qa Schema Generation");
            }

            return calculations;
        }

        private async Task<SpecificationSummary> GetSpecificationSummary(string specificationId)
        {
            ApiResponse<SpecificationSummary> specificationResponse = await _specificationResilience.ExecuteAsync(()
                => _specifications.GetSpecificationSummaryById(specificationId));

            return specificationResponse.Content;
        }

        private async Task<UniqueTemplateContents> GetTemplateData(SpecificationSummary specification, string fundingStreamId)
        {
            UniqueTemplateContents uniqueTemplateContents = new UniqueTemplateContents();

            TemplateMetadataContents templateMetadata = await GetTemplateMetadataContents(specification);

            if (templateMetadata == null)
            {
                throw new NonRetriableException(
                    $"Did not locate template information for specification {specification.Id} in {fundingStreamId}. Unable to complete Qa Schema Generation");
            }

            IEnumerable<FundingLine> flattenedFundingLines = templateMetadata.RootFundingLines.Flatten(_ => _.FundingLines)
                                                             ?? new FundingLine[0];

            IEnumerable<FundingLine> uniqueFundingLines = flattenedFundingLines.GroupBy(x => x.TemplateLineId)
                .Select(f => f.First());

            IEnumerable<Calculation> flattenedCalculations =
                flattenedFundingLines.SelectMany(_ => _.Calculations.Flatten(cal => cal.Calculations)) ?? Array.Empty<Calculation>();

            IEnumerable<Calculation> uniqueFlattenedCalculations =
                flattenedCalculations.GroupBy(x => x.TemplateCalculationId)
                    .Select(x => x.FirstOrDefault());

            uniqueTemplateContents.FundingLines = uniqueFundingLines;
            uniqueTemplateContents.Calculations = uniqueFlattenedCalculations;

            return uniqueTemplateContents;
        }

        private async Task<TemplateMetadataContents> GetTemplateMetadataContents(SpecificationSummary specificationSummary)
        {
            string fundingStreamId = specificationSummary.FundingStreams.FirstOrDefault()?.Id;
            string templateVersion = specificationSummary.TemplateIds[fundingStreamId];

            ApiResponse<FundingTemplateContents> templateContentsRequest = await _policiesResilience.ExecuteAsync(()
                => _policies.GetFundingTemplate(fundingStreamId, specificationSummary.FundingPeriod.Id, templateVersion));

            FundingTemplateContents fundingTemplateContents = templateContentsRequest.Content;

            string schemaVersion = fundingTemplateContents.SchemaVersion ?? fundingTemplateContents.Metadata?.SchemaVersion;

            ITemplateMetadataGenerator templateContents = _templateMetadataResolver.GetService(schemaVersion);

            return templateContents.GetMetadata(fundingTemplateContents.TemplateFileContents);
        }

        private ICosmosDbFeedIterator GetProviderResultsFeed(string specificationId)
            => _cosmos.GetFeedIterator(new CosmosDbQuery
            {
                QueryText = @"SELECT *
                                FROM p
                                WHERE p.documentType = 'ProviderResult'
                                AND p.content.specificationId = @specificationId
                                AND p.deleted = false",
                Parameters = Parameters(
                        ("@specificationId", specificationId))
            },
        100);

        private CosmosDbQueryParameter[] Parameters(params (string Name, object Value)[] parameters)
            => parameters.Select(_ => new CosmosDbQueryParameter(_.Name, _.Value)).ToArray();
    }
}
