using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.FeatureManagement;
using Polly;
using FundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;
using FundingLineType = CalculateFunding.Common.TemplateMetadata.Enums.FundingLineType;

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
        private readonly IReleaseManagementRepository _releaseManagementRepository;
        private readonly IFeatureManagerSnapshot _featureManagerSnapshot;
        private readonly IReleaseCandidateService _releaseCandidateService;

        public SqlImportContextBuilder(ICosmosRepository cosmos,
            IPoliciesApiClient policies,
            ITemplateMetadataResolver templateMetadataResolver,
            ISpecificationsApiClient specifications,
            IPublishingResiliencePolicies resiliencePolicies,
            IReleaseManagementRepository releaseManagementRepository,
            IFeatureManagerSnapshot featureManagerSnapshot,
            IReleaseCandidateService releaseCandidateService)
        {
            Guard.ArgumentNotNull(cosmos, nameof(cosmos));
            Guard.ArgumentNotNull(policies, nameof(policies));
            Guard.ArgumentNotNull(templateMetadataResolver, nameof(templateMetadataResolver));
            Guard.ArgumentNotNull(specifications, nameof(specifications));
            Guard.ArgumentNotNull(resiliencePolicies?.SpecificationsApiClient, nameof(resiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.PoliciesApiClient, nameof(resiliencePolicies.PoliciesApiClient));
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(releaseCandidateService, nameof(releaseCandidateService));

            _cosmos = cosmos;
            _policies = policies;
            _templateMetadataResolver = templateMetadataResolver;
            _specifications = specifications;
            _specificationResilience = resiliencePolicies.SpecificationsApiClient;
            _policiesResilience = resiliencePolicies.PoliciesApiClient;
            _releaseManagementRepository = releaseManagementRepository;
            _featureManagerSnapshot = featureManagerSnapshot;
            _releaseCandidateService = releaseCandidateService;
        }

        public async Task<ISqlImportContext> CreateImportContext(
            string specificationId,
            string fundingStreamId,
            SchemaContext schemaContext,
            SqlExportSource sqlExportSource)
        {
            ICosmosDbFeedIterator publishedProviderFeed = GetPublishedProviderFeed(specificationId, fundingStreamId, sqlExportSource);

            SpecificationSummary specification = await GetSpecificationSummary(specificationId);

            ICosmosDbFeedIterator releasedPublishedProviderVersionFeed
                = GetReleasedPublishedProviderVersionFeed(fundingStreamId, specification.FundingPeriod.Id);

            IEnumerable<ProviderVersionInChannel> providerVersionInChannels 
                = await GetProviderVersionInChannels(specification, fundingStreamId);

            TemplateMetadataContents template = await GetTemplateMetadataContents(specification, fundingStreamId);

            IEnumerable<FundingLine> allFundingLines = template.RootFundingLines.Flatten(_ => _.FundingLines);
            IEnumerable<FundingLine> informationFundingLines = allFundingLines.Where(_ => _.Type == FundingLineType.Information);
            IEnumerable<FundingLine> paymentFundingLines = allFundingLines.Where(_ => _.Type == FundingLineType.Payment);
            IEnumerable<Calculation> allCalculations = allFundingLines.SelectMany(_ => _.Calculations.Flatten(cal => cal.Calculations));
            IEnumerable<Calculation> uniqueCalculations = Enumerable.DistinctBy(allCalculations, _ => _.TemplateCalculationId);
            IDictionary<uint, string> calculationNames = GetCalculationNames(uniqueCalculations);

            bool isLatestReleasedVersionChannelPopulationEnabled 
                = await _featureManagerSnapshot.IsEnabledAsync("EnableLatestReleasedVersionChannelPopulation");

            return new SqlImportContext
            {
                CurrentPublishedProviderDocuments = publishedProviderFeed,
                ReleasedPublishedProviderVersionDocuments = releasedPublishedProviderVersionFeed,
                CalculationNames = calculationNames,
                Calculations = new CalculationDataTableBuilder(uniqueCalculations),
                Providers = new ProviderDataTableBuilder(),
                Funding = new PublishedProviderVersionDataTableBuilder(
                    _releaseCandidateService,
                    providerVersionInChannels, 
                    sqlExportSource,
                    isLatestReleasedVersionChannelPopulationEnabled),
                InformationFundingLines = new InformationFundingLineDataTableBuilder(informationFundingLines),
                PaymentFundingLines = new PaymentFundingLineDataTableBuilder(paymentFundingLines),
                ProviderPaymentFundingLineAllVersions = new ProviderPaymentFundingLineDataTableBuilder(),
                SchemaContext = schemaContext,
                SqlExportSource = sqlExportSource
            };
        }

        private static IDictionary<uint, string> GetCalculationNames(IEnumerable<Calculation> calculations)
            => calculations.ToDictionary(_ => _.TemplateCalculationId, _ => _.Name);

        private async Task<IEnumerable<ProviderVersionInChannel>> GetProviderVersionInChannels(
            SpecificationSummary specification,
            string fundingStreamId)
        {
            ApiResponse<FundingConfiguration> fundingConfigurationResponse = await _policiesResilience.ExecuteAsync(()
                => _policies.GetFundingConfiguration(fundingStreamId, specification.FundingPeriod.Id));

            FundingConfiguration fundingConfiguration = fundingConfigurationResponse.Content;

            IEnumerable<Channel> channels = await _releaseManagementRepository.GetChannels();

            IEnumerable<FundingConfigurationChannel> releaseChannels 
                = fundingConfiguration.ReleaseChannels.Where(_ => _.IsVisible);

            IEnumerable<ProviderVersionInChannel> providerVersionInChannels =
                await _releaseManagementRepository.GetLatestPublishedProviderVersions(
                    specification.Id, 
                    releaseChannels.Select(rc => channels.SingleOrDefault(c => c.ChannelCode == rc.ChannelCode).ChannelId));

            return providerVersionInChannels;
        }

        private async Task<TemplateMetadataContents> GetTemplateMetadataContents(SpecificationSummary specification,
            string fundingStreamId)
        {
            string templateVersion = specification.TemplateIds[fundingStreamId];

            ApiResponse<FundingTemplateContents> templateContentsRequest = await _policiesResilience.ExecuteAsync(()
                => _policies.GetFundingTemplate(fundingStreamId, specification.FundingPeriod.Id, templateVersion));

            FundingTemplateContents fundingTemplateContents = templateContentsRequest.Content;

            string schemaVersion = fundingTemplateContents.SchemaVersion ?? fundingTemplateContents.Metadata?.SchemaVersion;

            ITemplateMetadataGenerator templateContents = _templateMetadataResolver.GetService(schemaVersion);

            return templateContents.GetMetadata(fundingTemplateContents.TemplateFileContents);
        }

        private async Task<SpecificationSummary> GetSpecificationSummary(string specificationId)
        {
            ApiResponse<SpecificationSummary> specificationResponse = await _specificationResilience.ExecuteAsync(()
                => _specifications.GetSpecificationSummaryById(specificationId));

            SpecificationSummary specification = specificationResponse.Content;
            return specification;
        }

        private ICosmosDbFeedIterator GetPublishedProviderFeed(string specificationId,
            string fundingStreamId,
            SqlExportSource sqlExportSource)
            {
                return _cosmos.GetFeedIterator(new CosmosDbQuery
                {
                    QueryText = @$"SELECT
                                  *
                            FROM publishedProvider p
                            WHERE p.documentType = 'PublishedProvider'
                            {FilterQuery(sqlExportSource)}
                            AND p.content.current.fundingStreamId = @fundingStreamId
                            AND p.content.current.specificationId = @specificationId
                            AND p.deleted = false",
                    Parameters = Parameters(
                          ("@fundingStreamId", fundingStreamId),
                          ("@specificationId", specificationId))
                }, 100);
            }

        private ICosmosDbFeedIterator GetReleasedPublishedProviderVersionFeed(
            string fundingStreamId,
            string fundingPeriodId)
        {
            return _cosmos.GetFeedIterator(new CosmosDbQuery
            {
                QueryText = @$"SELECT
                                  *
                            FROM publishedProvider p
                            WHERE p.documentType = 'PublishedProviderVersion'
                            AND p.deleted = false                            
                            AND p.content.status = 'Released'
                            AND p.content.fundingStreamId = @fundingStreamId
                            AND p.content.fundingPeriodId = @fundingPeriodId
                            ",
                Parameters = Parameters(
                      ("@fundingStreamId", fundingStreamId),
                      ("@fundingPeriodId", fundingPeriodId))
            }, 100);
        }

        private static CosmosDbQueryParameter[] Parameters(params (string Name, object Value)[] parameters)
            => parameters.Select(_ => new CosmosDbQueryParameter(_.Name, _.Value)).ToArray();

        private static string FilterQuery(SqlExportSource sqlExportSource)
        {
            if(sqlExportSource == SqlExportSource.ReleasedPublishedProviderVersion)
            {
                return "AND p.content.released != null";
            }

            return string.Empty;
        }
    }
}