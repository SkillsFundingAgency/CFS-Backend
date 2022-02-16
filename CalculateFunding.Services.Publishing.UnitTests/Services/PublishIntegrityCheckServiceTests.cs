using AutoMapper;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Specifications;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Storage;
using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels.QueryResults;
using Microsoft.FeatureManagement;
using CalculationResult = CalculateFunding.Models.Publishing.CalculationResult;
using FundingLine = CalculateFunding.Models.Publishing.FundingLine;
using TemplateCalculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;
using TemplateFundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;
using VariationReason = CalculateFunding.Models.Publishing.VariationReason;

namespace CalculateFunding.Services.Publishing.UnitTests.Services
{
    [TestClass]
    public class PublishIntegrityCheckServiceTests : ServiceTestsBase
    {
        private PublishIntegrityCheckService _publishIntegrityService;
        private RandomString _jobId;
        private RandomString _correlationId;
        private Reference _author;
        private SpecificationSummary _specificationSummary;
        private IPublishingResiliencePolicies _publishingResiliencePolicies;
        private ISpecificationService _specificationService;
        private IPrerequisiteCheckerLocator _prerequisiteCheckerLocator;
        private IPublishedFundingChangeDetectorService _publishedFundingChangeDetectorService;
        private IPublishedFundingGenerator _publishedFundingGenerator;
        private IPublishedProviderContentsGeneratorResolver _publishedProviderContentsGeneratorResolver;
        private IPublishedFundingContentsPersistenceService _publishedFundingContentsPersistenceService;
        private IPublishedProviderContentPersistenceService _publishedProviderContentsPersistenceService;
        private ICalculationsApiClient _calculationsApiClient;
        private IProviderService _providerService;
        private IJobManagement _jobManagement;
        private IMapper _mapper;
        private IPublishedFundingService _publishedFundingService;
        private ILogger _logger;
        private ISpecificationsApiClient _specificationsApiClient;
        private TemplateMetadataContents _templateMetadataContents;
        private TemplateMapping _templateMapping;
        private TemplateFundingLine[] _fundingLines;
        private CalculationResult[] _calculationResults;
        private TemplateCalculation[] _calculationTemplateIds;
        private IEnumerable<PublishedProvider> _publishedProviders;
        private IPublishedFundingDataService _publishedFundingDataService;
        private IPoliciesService _policiesService;
        private IOrganisationGroupGenerator _organisationGroupGenerator;
        private IPublishedFundingDateService _publishedFundingDateService;
        private ISpecificationFundingStatusService _specificationFundingStatusService;
        private IPublishedFundingVersionDataService _publishedFundingVersionDataService;
        private IJobsRunning _jobsRunning;
        private ISearchRepository<PublishedFundingIndex> _publishedFundingSearchRepository;
        private IBlobClient _blobClient;
        private IFeatureManagerSnapshot _featureManager;
        private IReleaseManagementRepository _releaseManagementRepository;
        private IPublishedProviderContentChannelPersistenceService _publishedProviderContentChannelPersistenceService;
        private IPublishedFundingContentsChannelPersistenceService _publishedFundingContentsChannelPersistenceService;
        private IPublishedFundingRepository _publishedFundingRepository;

        private const string SpecificationId = "SpecificationId";
        private const string FundingPeriodId = "AY-2020";
        private const string JobId = "JobId";
        private const string FundingStreamId = "PSG";
        private const string CorrelationId = "CorrelationId";
        private const int ContractingChannelId = 1;
        private const int StatementChannelId = 2;

        private string _publishedProviderId;
        private string[] _publishedProviderIds;

        [TestInitialize]
        public void Setup()
        {
            _publishedProviderId = NewRandomString();
            _publishedProviderIds = new[] { _publishedProviderId };
            _publishingResiliencePolicies = new ResiliencePolicies
            {
                PublishedFundingRepository = Policy.NoOpAsync(),
                CalculationsApiClient = Policy.NoOpAsync(),
                SpecificationsApiClient = Policy.NoOpAsync(),
                SpecificationsRepositoryPolicy = Policy.NoOpAsync(),
                PublishedIndexSearchResiliencePolicy = Policy.NoOpAsync()
            };
            _specificationsApiClient = Substitute.For<ISpecificationsApiClient>();
            _specificationService = new SpecificationService(_specificationsApiClient, _publishingResiliencePolicies);
            _specificationFundingStatusService = Substitute.For<ISpecificationFundingStatusService>();
            _jobsRunning = Substitute.For<IJobsRunning>();
            _logger = Substitute.For<ILogger>();
            _publishedFundingChangeDetectorService = Substitute.For<IPublishedFundingChangeDetectorService>();
            _publishedFundingGenerator = Substitute.For<IPublishedFundingGenerator>();
            _publishedProviderContentsGeneratorResolver = Substitute.For<IPublishedProviderContentsGeneratorResolver>();
            _publishedFundingContentsPersistenceService = Substitute.For<IPublishedFundingContentsPersistenceService>();
            _publishedProviderContentsPersistenceService = Substitute.For<IPublishedProviderContentPersistenceService>();
            _calculationsApiClient = Substitute.For<ICalculationsApiClient>();
            _providerService = Substitute.For<IProviderService>();
            _jobManagement = Substitute.For<IJobManagement>();
            _prerequisiteCheckerLocator = Substitute.For<IPrerequisiteCheckerLocator>();
            _prerequisiteCheckerLocator.GetPreReqChecker(PrerequisiteCheckerType.ReleaseAllProviders)
                .Returns(new PublishAllPrerequisiteChecker(_specificationFundingStatusService, _jobsRunning, _jobManagement, _logger));
            _prerequisiteCheckerLocator.GetPreReqChecker(PrerequisiteCheckerType.ReleaseBatchProviders)
                .Returns(new PublishBatchPrerequisiteChecker(_specificationFundingStatusService, _jobsRunning, _jobManagement, _logger));
            _mapper = Substitute.For<IMapper>();
            _publishedFundingDataService = Substitute.For<IPublishedFundingDataService>();
            _policiesService = Substitute.For<IPoliciesService>();
            _organisationGroupGenerator = Substitute.For<IOrganisationGroupGenerator>();
            _publishedFundingChangeDetectorService = Substitute.For<IPublishedFundingChangeDetectorService>();
            _publishedFundingDateService = Substitute.For<IPublishedFundingDateService>();
            _publishedFundingDataService = Substitute.For<IPublishedFundingDataService>();
            _publishedFundingVersionDataService = Substitute.For<IPublishedFundingVersionDataService>();
            _publishedFundingSearchRepository = Substitute.For<ISearchRepository<PublishedFundingIndex>>();
            _blobClient = Substitute.For<IBlobClient>();
            _featureManager = Substitute.For<IFeatureManagerSnapshot>();
            _releaseManagementRepository = Substitute.For<IReleaseManagementRepository>();
            _publishedProviderContentChannelPersistenceService =
                Substitute.For<IPublishedProviderContentChannelPersistenceService>();
            _publishedFundingContentsChannelPersistenceService =
                Substitute.For<IPublishedFundingContentsChannelPersistenceService>();
            _publishedFundingRepository = Substitute.For<IPublishedFundingRepository>();

            _publishedFundingService = new PublishedFundingService(_publishedFundingDataService,
                _publishingResiliencePolicies,
                _policiesService,
                _organisationGroupGenerator,
                _publishedFundingChangeDetectorService,
                _publishedFundingDateService,
                _mapper,
                _logger);

            _publishIntegrityService = new PublishIntegrityCheckService(_jobManagement,
                _logger,
                _specificationService,
                _providerService,
                _publishedFundingContentsPersistenceService,
                _publishedProviderContentsPersistenceService,
                _publishingResiliencePolicies,
                _publishedFundingDataService,
                _policiesService,
                _calculationsApiClient,
                _publishedFundingService,
                _publishedProviderContentsGeneratorResolver,
                _publishedFundingSearchRepository,
                _publishedFundingVersionDataService,
                _releaseManagementRepository,
                _featureManager,
                _blobClient,
                _publishedProviderContentChannelPersistenceService,
                _publishedFundingContentsChannelPersistenceService,
                _publishedFundingRepository
            );

            _jobId = NewRandomString();
            _correlationId = NewRandomString();
            _author = new Reference( NewRandomString(), new RandomString());
        }

        [TestMethod]
        public async Task Run_PersistsProviderFundingAndPublishedProviders_Legacy()
        {
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndPublishedProviders();
            AndTemplateMapping();
            AndGeneratePublishedFunding();

            await WhenPublishIntegrityCheckMessageReceivedWithJobId();

            ThenPublishedFundingSaved();

            ThenPublishedProvidersSaved();
        }

        [TestMethod]
        public async Task Run_PersistsProviderFundingAndPublishedProvidersForBatch_Legacy()
        {
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndPublishedProviders();
            AndTemplateMapping();
            AndGeneratePublishedFunding();

            await WhenPublishIntegrityCheckMessageReceivedWithJobIdAndBatchedProviders(BuildPublishProvidersRequest(_ => _.WithProviders(_publishedProviderIds)));

            ThenPublishedFundingSaved();

            ThenPublishedProvidersSaved(providers: _publishedProviderIds);
        }

        [TestMethod]
        public async Task Run_PersistsProviderFundingAndPublishedProviders()
        {
            GivenJobCanBeProcessed();
            AndNewReleaseManagement();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndPublishedProviders();
            AndTemplateMapping();
            AndGeneratePublishedFunding();

            await WhenPublishIntegrityCheckMessageReceivedWithJobId();

            ThenPublishedFundingSaved(isLegacy: false);

            ThenPublishedProvidersSaved(isLegacy: false);
        }

        [TestMethod]
        public async Task Run_PersistsProviderFundingAndPublishedProvidersForBatch()
        {
            GivenJobCanBeProcessed();
            AndNewReleaseManagement();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndPublishedProviders();
            AndTemplateMapping();
            AndGeneratePublishedFunding();

            await WhenPublishIntegrityCheckMessageReceivedWithJobIdAndBatchedProviders(BuildPublishProvidersRequest(_ => _.WithProviders(_publishedProviderIds)));

            ThenPublishedFundingSaved(isLegacy: false);

            ThenPublishedProvidersSaved(isLegacy: false, providers: _publishedProviderIds);
        }

        private void AndNewReleaseManagement()
        {
            _featureManager.IsEnabledAsync(Arg.Is<string>("EnableReleaseManagementBackend"))
                .Returns(true);

            _releaseManagementRepository.GetChannels()
                .Returns(new List<Channel>
                {
                    new Channel { ChannelId = ContractingChannelId, ChannelCode = "Contracting", ChannelName = "Contracting" },
                    new Channel { ChannelId = StatementChannelId, ChannelCode = "Statement", ChannelName = "Statement" }
                });
        }

        private void ThenPublishedFundingSaved(bool isLegacy = true)
        {
            if (isLegacy)
            {
                _publishedFundingContentsPersistenceService
                    .Received(1)
                    .SavePublishedFundingContents(
                        Arg.Is<IEnumerable<PublishedFundingVersion>>(
                            _ => _.Count() == _publishedProviders.Count()),
                        Arg.Is(_templateMetadataContents)
                    );
            }
            else
            {
                _publishedFundingContentsChannelPersistenceService
                    .Received(1)
                    .SavePublishedFundingContents(
                        Arg.Is<IEnumerable<PublishedFundingVersion>>(
                            _ => _.Count() == _publishedProviders.Count()),
                        Arg.Is<Channel>(_ => _.ChannelId == ContractingChannelId));
            }
        }

        private void ThenPublishedProvidersSaved(bool isLegacy = true, IEnumerable<string> providers = null)
        {
            if (isLegacy)
            {
                providers ??= _publishedProviders.Select(_ => _.Id);
                _publishedProviderContentsPersistenceService
                    .Received(1)
                    .SavePublishedProviderContents(
                        Arg.Is(_templateMetadataContents),
                        Arg.Is(_templateMapping),
                        Arg.Is<IEnumerable<PublishedProvider>>(
                            _ => _.All(ppv => providers.ToDictionary(_ => _)
                                .ContainsKey(ppv.Id))),
                        Arg.Any<IPublishedProviderContentsGenerator>()
                    );
            }
            else
            {
                providers ??= _publishedProviders.Select(_ => _.Id);
                _publishedProviderContentChannelPersistenceService
                    .Received(1)
                    .SavePublishedProviderContents(
                        Arg.Is(_templateMapping),
                        Arg.Is<IEnumerable<PublishedProviderVersion>>(
                            _ => _.Count() == providers.Count()),
                        Arg.Is<Channel>(_ => _.ChannelId == ContractingChannelId));
            }
        }

        private void GivenJobCanBeProcessed()
        {
            JobViewModel jobViewModel = NewJobViewModel();

            _jobManagement.RetrieveJobAndCheckCanBeProcessed(JobId)
                .Returns(jobViewModel);
        }

        private void AndSpecification()
        {
            _specificationSummary = NewSpecificationSummary(_ => _.WithId(SpecificationId)
            .WithFundingStreamIds(new[] { FundingStreamId })
            .WithFundingPeriodId(FundingPeriodId)
            .WithTemplateIds((FundingStreamId, "1.0")));

            _specificationsApiClient.GetSpecificationSummaryById(SpecificationId)
                .Returns(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, _specificationSummary));
        }

        private void AndTemplateMetadataContents()
        {
            _calculationTemplateIds = new[] { NewTemplateCalculation(), 
                NewTemplateCalculation(), 
                NewTemplateCalculation() };

            _fundingLines = new[] { NewTemplateFundingLine(fl => fl.WithCalculations(_calculationTemplateIds)) };

            _templateMetadataContents = NewTemplateMetadataContents(_ => _.WithFundingLines(_fundingLines));

            _policiesService
                .GetTemplateMetadataContents(FundingStreamId, _specificationSummary.FundingPeriod.Id,
                                             _specificationSummary.TemplateIds[FundingStreamId])
                .Returns(_templateMetadataContents);
        }

        private TemplateCalculation NewTemplateCalculation(Action<TemplateCalculationBuilder> setUp = null)
        {
            TemplateCalculationBuilder templateCalculationBuilder = new TemplateCalculationBuilder();

            setUp?.Invoke(templateCalculationBuilder);
            
            return templateCalculationBuilder.Build();
        }

        private void AndTemplateMapping()
        {
            TemplateMappingItem[] templateMappingItems = new[] {
                NewTemplateMappingItem(_ => _.WithTemplateId(_calculationTemplateIds[0].TemplateCalculationId)
                .WithCalculationId(_calculationResults[0].Id)),
                NewTemplateMappingItem(_ => _.WithTemplateId(_calculationTemplateIds[1].TemplateCalculationId)
                    .WithCalculationId(_calculationResults[1].Id)),
                NewTemplateMappingItem(_ => _.WithTemplateId(_calculationTemplateIds[2].TemplateCalculationId)
                    .WithCalculationId(_calculationResults[2].Id))
                };
            
            _templateMapping = NewTemplateMapping(_ => _.WithItems(templateMappingItems));

            _calculationsApiClient
                .GetTemplateMapping(_specificationSummary.Id, FundingStreamId)
                .Returns(new ApiResponse<TemplateMapping>(HttpStatusCode.OK, _templateMapping));
        }
        
        private void AndPublishedProviders()
        {
            Provider[] providers = new[] { NewProvider(_ => _.WithProviderId(_publishedProviderId)), 
                NewProvider(), 
                NewProvider()
            };

            TemplateFundingLine templateFundingLine = _fundingLines[0];

            _publishedProviders = providers.Select(_ =>
                    NewPublishedProvider(pp => pp.WithCurrent(
                        NewPublishedProviderVersion(ppv => ppv
                            .WithProvider(_)
                            .WithProviderId(_.ProviderId)
                            .WithTotalFunding(9)
                            .WithFundingLines(NewFundingLine(fl => fl.WithFundingLineCode(templateFundingLine.FundingLineCode)
                                .WithTemplateLineId(templateFundingLine.TemplateLineId)
                                .WithValue(9)))
                            .WithFundingCalculations(NewFundingCalculation(fc => fc.WithTemplateCalculationId(_calculationTemplateIds[0].TemplateCalculationId)
                                .WithValue(_calculationTemplateIds[0].Value)),
                                NewFundingCalculation(fc => fc.WithTemplateCalculationId(_calculationTemplateIds[1].TemplateCalculationId)
                                    .WithValue(_calculationTemplateIds[1].Value)),
                                NewFundingCalculation(fc => fc.WithTemplateCalculationId(_calculationTemplateIds[2].TemplateCalculationId)
                                    .WithValue(_calculationTemplateIds[2].Value)))
                            .WithPublishedProviderStatus(PublishedProviderStatus.Released)
                            .WithVariationReasons(new[] { VariationReason.AuthorityFieldUpdated })
                            )))).ToList();

            _providerService
                .GetPublishedProviders(Arg.Is<Reference>(_ => _.Id == FundingStreamId),
                                                         _specificationSummary)
                .Returns((_publishedProviders.ToDictionary(_ => _.Current.ProviderId),
                                                           _publishedProviders.ToDictionary(_ => _.Current.ProviderId)));

            _releaseManagementRepository.GetReleasedProviderSummaryBySpecificationId(Arg.Is<string>(SpecificationId))
                .Returns(_publishedProviders
                    .Select(_ => _.Current)
                    .Select(_ => new ReleasedProviderSummary
                    {
                        ChannelId = ContractingChannelId,
                        FundingId = _.FundingId,
                        ProviderId = _.ProviderId
                    })
                    .ToList());

            _releaseManagementRepository.GetLatestReleasedProviderSummaryBySpecificationId(Arg.Is<string>(SpecificationId))
                .Returns(_publishedProviders
                    .Select(_ => _.Current)
                    .Select(_ => new ReleasedProviderSummary
                    {
                        ChannelId = ContractingChannelId,
                        FundingId = _.FundingId,
                        ProviderId = _.ProviderId
                    })
                    .ToList());

            _publishedFundingRepository.GetPublishedProviderVersions(Arg.Is<string>(FundingStreamId),
                    Arg.Is<string>(FundingPeriodId), Arg.Any<string>())
                .Returns(_publishedProviders
                    .Select(_ => _.Current)
                    .ToList());
        }

        private FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);
            
            return fundingLineBuilder.Build();
        }

        private FundingCalculation NewFundingCalculation(Action<FundingCalculationBuilder> setUp = null)
        {
            FundingCalculationBuilder fundingCalculationBuilder = new FundingCalculationBuilder();

            setUp?.Invoke(fundingCalculationBuilder);
            
            return fundingCalculationBuilder.Build();
        }

        private TemplateMappingItem NewTemplateMappingItem(Action<TemplateMappingItemBuilder> setUp = null)
        {
            TemplateMappingItemBuilder templateMappingItemBuilder = new TemplateMappingItemBuilder();

            setUp?.Invoke(templateMappingItemBuilder);
            
            return templateMappingItemBuilder.Build();
        }

        private PublishedFundingInput NewPublishedFundingInput(Action<PublishedFundingInputBuilder> setUp = null)
        {
            PublishedFundingInputBuilder publishedFundingInputBuilder = new PublishedFundingInputBuilder();

            setUp?.Invoke(publishedFundingInputBuilder);

            return publishedFundingInputBuilder.Build();
        }

        private PublishedFunding NewPublishedFunding(Action<PublishedFundingBuilder> setUp = null)
        {
            PublishedFundingBuilder publishedFundingBuilder = new PublishedFundingBuilder();

            setUp?.Invoke(publishedFundingBuilder);

            return publishedFundingBuilder.Build();
        }

        private PublishedFundingVersion NewPublishedFundingVersion(Action<PublishedFundingVersionBuilder> setUp = null)
        {
            PublishedFundingVersionBuilder publishedFundingVersionBuilder = new PublishedFundingVersionBuilder();

            setUp?.Invoke(publishedFundingVersionBuilder);

            return publishedFundingVersionBuilder.Build();
        }

        private void AndGeneratePublishedFunding()
        {
            IEnumerable<PublishedFundingVersion> publishedFundingVersions = _publishedProviders.Select(_ =>
                NewPublishedFundingVersion(pfv =>
                    pfv.WithSpecificationId(SpecificationId)
                        .WithFundingStreamId(FundingStreamId)
                        .WithFundingPeriod(new PublishedFundingPeriod { Id = FundingPeriodId })
                        .WithGroupReason(CalculateFunding.Models.Publishing.GroupingReason.Contracting)
                        .WithOrganisationGroupIdentifierValue("value")
                        .WithOrganisationGroupTypeCode(OrganisationGroupTypeCode.AcademyTrust)
                        .WithMajor(1)
                        .WithMinor(0)
                    ))
                .ToList();

            IEnumerable<(PublishedFunding, PublishedFundingVersion)> publishedFunding = publishedFundingVersions.Select(_ =>
                (NewPublishedFunding(pf => pf.WithCurrent(_)),_)
            ).ToList();

            _publishedFundingGenerator
                .GeneratePublishedFunding(
                    Arg.Is<PublishedFundingInput>(_ => _.SpecificationId == SpecificationId),
                    Arg.Is<ICollection<PublishedProvider>>(_ => _.All(pp => _publishedProviders.Select(pp => pp.Current.ProviderId).Contains(pp.Current.ProviderId))),
                    _author,
                    _jobId,
                    _correlationId
                    )
                .Returns(publishedFunding);

            _publishedFundingVersionDataService.GetPublishedFundingVersion(Arg.Is<string>(FundingStreamId), Arg.Is<string>(FundingPeriodId))
                .Returns(publishedFunding.Select(_ => _.Item2).ToList());

            _publishedFundingDataService
                .GetCurrentPublishedFunding(Arg.Is<string>(FundingStreamId), Arg.Is<string>(FundingPeriodId))
                .Returns(publishedFunding.Select(_ => _.Item1).ToList());

            IEnumerable<FundingGroupVersion> fundingGroupVersions = publishedFunding
                .Select(_ => _.Item2)
                .Select(_ => new FundingGroupVersion
                {
                    FundingId = _.FundingId,
                    ChannelId = ContractingChannelId
                }
                ).ToList();

            _releaseManagementRepository.GetFundingGroupVersionsBySpecificationId(Arg.Is<string>(SpecificationId))
                .Returns(fundingGroupVersions);

            _releaseManagementRepository.GetLatestFundingGroupVersionsBySpecificationId(Arg.Is<string>(SpecificationId))
                .Returns(fundingGroupVersions);
        }

        private void AndCalculationResultsBySpecificationId()
        {
            decimal[] results = new[] { 2M, 3M, 4M };

            _calculationResults = results.Select(res => NewCalculationResult(cr => cr.WithValue(res))).ToArray();
        }

        private async Task WhenPublishIntegrityCheckMessageReceivedWithJobId()
        {
            Message message = NewMessage(_ => _.WithUserProperty("specification-id", 
                                                                 SpecificationId)
                .WithUserProperty("jobId", JobId)
                .WithUserProperty("sfa-correlationId", CorrelationId));

            await _publishIntegrityService.Run(message);
        }

        private async Task WhenPublishIntegrityCheckMessageReceivedWithJobIdAndBatchedProviders(PublishedProviderIdsRequest publishProvidersRequest)
        {
            Message message = NewMessage(_ => _
                .WithUserProperty("specification-id",SpecificationId)
                .WithUserProperty("jobId",JobId)
                .WithUserProperty("sfa-correlationId", CorrelationId)
                .WithUserProperty("providers-batch", publishProvidersRequest.PublishedProviderIds.AsJson()));

            await _publishIntegrityService.Run(message);
        }

        private PublishedProviderIdsRequest BuildPublishProvidersRequest(Action<PublishedProviderIdsRequestBuilder> setUp = null)
        {
            PublishedProviderIdsRequestBuilder publishProvidersRequestBuilder = new PublishedProviderIdsRequestBuilder();

            setUp?.Invoke(publishProvidersRequestBuilder);

            return publishProvidersRequestBuilder.Build();
        }

        private static RandomString NewRandomString() => new RandomString();
    }
}
