using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using FundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class SqlImportContextBuilder : ISqlImportContextBuilder
    {
        private readonly ICosmosRepository _cosmos;
        private readonly IPoliciesApiClient _policies;
        private readonly ITemplateMetadataResolver _templateMetadataResolver;
        private readonly ISpecificationsApiClient _specifications;
        private readonly AsyncPolicy _specificationResilience;
        private readonly AsyncPolicy _policiesResilience;

        public SqlImportContextBuilder(ICosmosRepository cosmos,
            IPoliciesApiClient policies,
            ITemplateMetadataResolver templateMetadataResolver,
            ISpecificationsApiClient specifications,
            IPublishingResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(cosmos, nameof(cosmos));
            Guard.ArgumentNotNull(policies, nameof(policies));
            Guard.ArgumentNotNull(templateMetadataResolver, nameof(templateMetadataResolver));
            Guard.ArgumentNotNull(specifications, nameof(specifications));
            Guard.ArgumentNotNull(resiliencePolicies?.SpecificationsApiClient, nameof(resiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.PoliciesApiClient, nameof(resiliencePolicies.PoliciesApiClient));
            
            _cosmos = cosmos;
            _policies = policies;
            _templateMetadataResolver = templateMetadataResolver;
            _specifications = specifications;
            _specificationResilience = resiliencePolicies.SpecificationsApiClient;
            _policiesResilience = resiliencePolicies.PoliciesApiClient;
        }

        public async Task<ISqlImportContext> CreateImportContext(string specificationId,
            string fundingStreamId,
            SchemaContext schemaContext)
        {
            ICosmosDbFeedIterator publishedProviderFeed = GetPublishedProviderFeed(specificationId, fundingStreamId);

            TemplateMetadataContents template = await GetTemplateMetadataContents(specificationId, fundingStreamId);

            IEnumerable<FundingLine> allFundingLines = template.RootFundingLines.Flatten(_ => _.FundingLines);
            IEnumerable<Calculation> allCalculations = allFundingLines.SelectMany(_ => _.Calculations.Flatten(cal => cal.Calculations));
            IEnumerable<Calculation> uniqueCalculations = allCalculations.DistinctBy(_ => _.TemplateCalculationId);
            IDictionary<uint, string> calculationNames = GetCalculationNames(uniqueCalculations);

            return new SqlImportContext
            {
                Documents = publishedProviderFeed,
                CalculationNames = calculationNames,
                Calculations = new CalculationDataTableBuilder(uniqueCalculations),
                Providers = new ProviderDataTableBuilder(),
                Funding = new PublishedProviderVersionDataTableBuilder(),
                InformationFundingLines = new InformationFundingLineDataTableBuilder(),
                PaymentFundingLines = new PaymentFundingLineDataTableBuilder(),
                SchemaContext = schemaContext
            };
        }

        private IDictionary<uint, string> GetCalculationNames(IEnumerable<Calculation> calculations)
            => calculations.ToDictionary(_ => _.TemplateCalculationId, _ => _.Name);

        private async Task<TemplateMetadataContents> GetTemplateMetadataContents(string specificationId,
            string fundingStreamId)
        {
            ApiResponse<SpecificationSummary> specificationResponse = await _specificationResilience.ExecuteAsync(() 
                => _specifications.GetSpecificationSummaryById(specificationId));

            SpecificationSummary specification = specificationResponse.Content;

            string templateVersion = specification.TemplateIds[fundingStreamId];

            ApiResponse<FundingTemplateContents> templateContentsRequest = await _policiesResilience.ExecuteAsync(() 
                => _policies.GetFundingTemplate(fundingStreamId, specification.FundingPeriod.Id, templateVersion));

            FundingTemplateContents fundingTemplateContents = templateContentsRequest.Content;

            string schemaVersion = fundingTemplateContents.SchemaVersion ?? fundingTemplateContents.Metadata?.SchemaVersion;

            ITemplateMetadataGenerator templateContents = _templateMetadataResolver.GetService(schemaVersion);

            return templateContents.GetMetadata(fundingTemplateContents.TemplateFileContents);
        }

        private ICosmosDbFeedIterator GetPublishedProviderFeed(string specificationId,
            string fundingStreamId)
            => _cosmos.GetFeedIterator(new CosmosDbQuery
                {
                    QueryText = @"SELECT
                              *
                        FROM publishedProvider p
                        WHERE p.documentType = 'PublishedProvider'
                        AND p.content.current.fundingStreamId = @fundingStreamId
                        AND p.content.current.specificationId = @specificationId
                        AND p.deleted = false",
                    Parameters = Parameters(
                        ("@fundingStreamId", fundingStreamId),
                        ("@specificationId", specificationId))
                },
                100);

        private CosmosDbQueryParameter[] Parameters(params (string Name, object Value)[] parameters)
            => parameters.Select(_ => new CosmosDbQueryParameter(_.Name, _.Value)).ToArray();
    }
}