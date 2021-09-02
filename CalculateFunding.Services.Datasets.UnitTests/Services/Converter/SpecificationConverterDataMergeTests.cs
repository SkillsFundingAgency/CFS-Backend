using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Datasets.Builders;
using CalculateFunding.Services.Datasets.Converter;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Tests.Common.Builders;
using CalculateFunding.Tests.Common.Helpers;
using CalculateFunding.UnitTests.ApiClientHelpers.Policies;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog.Core;
using static CalculateFunding.Tests.Common.Helpers.ConstraintHelpers;

namespace CalculateFunding.Services.Datasets.Services.Converter
{
    [TestClass]
    public class SpecificationConverterDataMergeTests
    {
        private SpecificationConverterDataMerge _specificationConverterDataMerge;
        private Mock<ISpecificationsApiClient> _specifications;
        private Mock<IPoliciesApiClient> _policies;
        private Mock<IDatasetRepository> _datasets;
        private Mock<IJobManagement> _jobs;

        [TestInitialize]
        public void SetUp()
        {
            _specifications = new Mock<ISpecificationsApiClient>();
            _policies = new Mock<IPoliciesApiClient>();
            _datasets = new Mock<IDatasetRepository>();
            _jobs = new Mock<IJobManagement>();

            _specificationConverterDataMerge = new SpecificationConverterDataMerge(_specifications.Object,
                _policies.Object,
                _datasets.Object,
                new DatasetsResiliencePolicies
                {
                    SpecificationsApiClient = Policy.NoOpAsync(),
                    PoliciesApiClient = Policy.NoOpAsync(),
                    DatasetRepository = Policy.NoOpAsync()
                },
                _jobs.Object,
                Logger.None);
        }

        [TestMethod]
        public void GuardsAgainstMissingRequestWhenQueueingJobs()
        {
            Func<Task<IActionResult>> invocation = () => WhenTheJobIsQueued(null);

            invocation
                .Should()
                .ThrowAsync<ArgumentNullException>()
                .Result
                .Which
                .ParamName
                .Should()
                .Be("request");
        }

        [TestMethod]
        public void GuardsAgainstMissingSpecificationIdInRequestWhenQueueingJobs()
        {
            Func<Task<IActionResult>> invocation = () => WhenTheJobIsQueued(NewSpecificationMergeRequest());

            invocation
                .Should()
                .ThrowAsync<ArgumentNullException>()
                .Result
                .Which
                .ParamName
                .Should()
                .Be("specificationId");
        }

        [TestMethod]
        public async Task CreatesParentQueueConverterDatasetMergeJobWhenQueueingJobs()
        {
            string specificationId = NewRandomString();
            Reference author = NewReference();

            SpecificationConverterMergeRequest request = NewSpecificationMergeRequest(_ => _.WithSpecificationId(specificationId)
                .WithAuthor(author));

            Job expectedJob = new Job
            {
                Id = NewRandomString()
            };

            GivenTheJobIsQueued(new JobCreateModel
            {
                JobDefinitionId = JobConstants.DefinitionNames.QueueConverterDatasetMergeJob,
                SpecificationId = specificationId,
                MessageBody = request.AsJson(),
                InvokerUserId = author.Id,
                InvokerUserDisplayName = author.Name,
                Properties = new Dictionary<string, string>
                    {
                        {"specification-id", specificationId}
                    },
                Trigger = new Trigger
                {
                    EntityId = specificationId,
                    EntityType = "Specification"
                }
            },
                expectedJob);

            JobCreationResponse result = (await WhenTheJobIsQueued(request) as OkObjectResult).Value as JobCreationResponse;

            result?.JobId
                .Should()
                .Be(expectedJob.Id);
        }

        [TestMethod]
        public void GuardsAgainstMissingMessageWhenProcessesJob()
        {
            Func<Task> invocation = () => WhenTheMessageIsProcessed(null);

            invocation
                .Should()
                .ThrowAsync<NonRetriableException>()
                .Result
                .Which
                .Message
                .Should()
                .Be("No message to process");
        }

        [TestMethod]
        public void GuardsAgainstMissingRequestInMessageBodyWhenProcessesJob()
        {
            Func<Task> invocation = () => WhenTheMessageIsProcessed(NewMessage());

            invocation
                .Should()
                .ThrowAsync<NonRetriableException>()
                .Result
                .Which
                .Message
                .Should()
                .Be("Message does not contain a SpecificationConverterMergeRequest to process");
        }

        [TestMethod]
        public void GuardsAgainstMissingSpecificationForSuppliedIdWhenProcessesJob()
        {
            Func<Task> invocation = () => WhenTheMessageIsProcessed(NewMessage(_ =>
                _.WithMessageBody(NewSpecificationMergeRequest().AsJsonBytes())));

            invocation
                .Should()
                .ThrowAsync<NonRetriableException>()
                .Result
                .Which
                .Message
                .Should()
                .StartWith("Did not locate specification with Id ");
        }

        [TestMethod]
        public void GuardsAgainstMissingFundingConfigurationForSpecificationWhenProcessesJob()
        {
            string specificationId = NewRandomString();

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithId(specificationId)
                .WithFundingPeriodId(NewRandomString())
                .WithFundingStreamIds(NewRandomString()));

            GivenTheSpecification(specificationId, specificationSummary);

            SpecificationConverterMergeRequest specificationMergeRequest = NewSpecificationMergeRequest(_
                => _.WithSpecificationId(specificationId));

            Func<Task> invocation = () => WhenTheMessageIsProcessed(NewMessage(_ =>
                _.WithMessageBody(specificationMergeRequest.AsJsonBytes())));

            invocation
                .Should()
                .ThrowAsync<NonRetriableException>()
                .Result
                .Which
                .Message
                .Should()
                .StartWith("Did not locate a funding configuration for ");
        }

        [TestMethod]
        public async Task DoesNotQueueAnyChildJobsIfFundingConfigurationForSpecificationDoesNotPermitConverterWizard()
        {
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithId(specificationId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithFundingStreamIds(fundingStreamId));

            FundingConfiguration fundingConfiguration = NewFundingConfiguration(_
                => _.WithEnableConverterDataMerge(false));

            GivenTheSpecification(specificationId, specificationSummary);
            AndTheFundingConfiguration(fundingStreamId, fundingPeriodId, fundingConfiguration);

            SpecificationConverterMergeRequest specificationMergeRequest = NewSpecificationMergeRequest(_
                => _.WithSpecificationId(specificationId));

            await WhenTheMessageIsProcessed(NewMessage(_ =>
                _.WithMessageBody(specificationMergeRequest.AsJsonBytes())));

            ThenNoJobsWereQueued();

            AndTheJobAutoCompleteIsSetTo(true);
        }

        [TestMethod]
        public async Task QueuesChildJobForEachRelatedDatasetForSpecificationWhenProcessesJob()
        {
            string parentJobId = NewRandomString();

            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithId(specificationId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithFundingStreamIds(fundingStreamId)
                .WithProviderVersionId(NewRandomString()));

            FundingConfiguration fundingConfiguration = NewFundingConfiguration(_
                => _.WithEnableConverterDataMerge(true));

            GivenTheSpecification(specificationId, specificationSummary);
            AndTheFundingConfiguration(fundingStreamId, fundingPeriodId, fundingConfiguration);
            AndTheParentJob(parentJobId);

            Reference author = NewReference();

            SpecificationConverterMergeRequest specificationMergeRequest = NewSpecificationMergeRequest(_
                => _.WithSpecificationId(specificationId)
                    .WithAuthor(author));

            DefinitionSpecificationRelationship relationshipOne = NewDefinitionSpecificationRelationship(r => r.WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ => _.WithConverterEnabled(false))));
            DefinitionSpecificationRelationship relationshipTwo = NewDefinitionSpecificationRelationship(r => r.WithCurrent(NewDefinitionSpecificationRelationshipVersion()));
            DefinitionSpecificationRelationship relationshipThree = NewDefinitionSpecificationRelationship(r => r.WithCurrent(NewDefinitionSpecificationRelationshipVersion()));

            AndTheDefinitionSpecificationRelationships(specificationId, relationshipOne, relationshipTwo, relationshipThree);

            await WhenTheMessageIsProcessed(NewMessage(_ =>
                _.WithMessageBody(specificationMergeRequest.AsJsonBytes())));

            ThenTheJobsWereQueued(NewConverterMergeJobCreateModel(parentJobId, relationshipTwo, specificationSummary, author),
                NewConverterMergeJobCreateModel(parentJobId, relationshipThree, specificationSummary, author));
            AndTheJobsWereNotQueued(NewConverterMergeJobCreateModel(parentJobId, relationshipOne, specificationSummary, author));

            AndTheJobAutoCompleteIsSetTo(false);
        }

        [TestMethod]
        public async Task NoJobsQueuedAndCurrentJobCompletedForSpecificationWhenProcessesJob()
        {
            string parentJobId = NewRandomString();

            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithId(specificationId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithFundingStreamIds(fundingStreamId)
                .WithProviderVersionId(NewRandomString()));

            FundingConfiguration fundingConfiguration = NewFundingConfiguration(_
                => _.WithEnableConverterDataMerge(true));

            GivenTheSpecification(specificationId, specificationSummary);
            AndTheFundingConfiguration(fundingStreamId, fundingPeriodId, fundingConfiguration);
            AndTheParentJob(parentJobId);

            Reference author = NewReference();

            SpecificationConverterMergeRequest specificationMergeRequest = NewSpecificationMergeRequest(_
                => _.WithSpecificationId(specificationId)
                    .WithAuthor(author));

            DefinitionSpecificationRelationship relationshipOne = NewDefinitionSpecificationRelationship(r => r.WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ => _.WithConverterEnabled(false))));
            
            AndTheDefinitionSpecificationRelationships(specificationId, relationshipOne);

            await WhenTheMessageIsProcessed(NewMessage(_ =>
                _.WithMessageBody(specificationMergeRequest.AsJsonBytes())));

            AndTheJobsWereNotQueued(NewConverterMergeJobCreateModel(parentJobId, relationshipOne, specificationSummary, author));

            AndTheJobAutoCompleteIsSetTo(true);
        }

        private static JobCreateModel NewConverterMergeJobCreateModel(string parentJobId,
            DefinitionSpecificationRelationship relationship,
            SpecificationSummary specificationSummary,
            Reference author) =>
            new JobCreateModel
            {
                JobDefinitionId = JobConstants.DefinitionNames.RunConverterDatasetMergeJob,
                ParentJobId = parentJobId,
                Properties = new Dictionary<string, string>
                {
                    {"dataset-relationship-id", relationship.Id}
                },
                Trigger = new Trigger
                {
                    EntityId = relationship.Current.Specification.Id,
                    EntityType = "Specification",
                },
                MessageBody = new ConverterMergeRequest
                {
                    DatasetRelationshipId = relationship.Id,
                    DatasetId = relationship.Current.DatasetVersion.Id,
                    Version = relationship.Current.DatasetVersion.Version.ToString(),
                    ProviderVersionId = specificationSummary.ProviderVersionId,
                    Author = author
                }.AsJson()
            };

        private async Task WhenTheMessageIsProcessed(Message message)
            => await _specificationConverterDataMerge.Process(message);

        private Message NewMessage(Action<MessageBuilder> setUp = null)
        {
            MessageBuilder messageBuilder = new MessageBuilder();

            setUp?.Invoke(messageBuilder);

            return messageBuilder.Build();
        }

        private void ThenNoJobsWereQueued()
            => _jobs.Verify(_ => _.QueueJob(It.IsAny<JobCreateModel>()),
                Times.Never);

        private void ThenTheJobsWereQueued(params JobCreateModel[] jobs)
        {
            foreach (JobCreateModel expectedJob in jobs)
            {
                _jobs.Verify(_ => _.QueueJob(It.Is<JobCreateModel>(actualJob =>
                        AreEquivalent(actualJob, expectedJob))),
                    Times.Once);
            }
        }

        private void AndTheJobsWereNotQueued(params JobCreateModel[] jobs)
        {
            foreach (JobCreateModel expectedJob in jobs)
            {
                _jobs.Verify(_ => _.QueueJob(It.Is<JobCreateModel>(actualJob =>
                        AreEquivalent(actualJob, expectedJob))),
                    Times.Never);
            }
        }

        private void AndTheJobAutoCompleteIsSetTo(bool autoComplete)
        {
            _specificationConverterDataMerge.AutoComplete
                .Should()
                .Be(autoComplete);
        }

        private void GivenTheJobIsQueued(JobCreateModel expectedJobCreateModel,
            Job job)
            => _jobs.Setup(_ => _.QueueJob(It.Is<JobCreateModel>(actualJobCreateModel =>
                    AreEquivalent(actualJobCreateModel, expectedJobCreateModel))))
                .ReturnsAsync(job);

        private void AndTheDefinitionSpecificationRelationships(string specificationId,
            params DefinitionSpecificationRelationship[] relationships)
            => _datasets.Setup(_ => _.GetDefinitionSpecificationRelationshipsByQuery(It.Is<Expression<Func<DocumentEntity<DefinitionSpecificationRelationship>, bool>>>(query
                    => BooleanExpressionMatches(query,
                        NewDocument(NewDefinitionSpecificationRelationship(r => r.WithCurrent(NewDefinitionSpecificationRelationshipVersion(relationship
                            => relationship.WithSpecification(
                                NewReference(reference
                                    => reference.WithId(specificationId)))))))))))
                .ReturnsAsync(relationships);

        private void AndTheFundingConfiguration(string fundingStreamId,
            string fundingPeriodId,
            FundingConfiguration fundingConfiguration)
            => _policies.Setup(_ => _.GetFundingConfiguration(fundingStreamId, fundingPeriodId))
                .ReturnsAsync(new ApiResponse<FundingConfiguration>(HttpStatusCode.OK, fundingConfiguration));

        private void GivenTheSpecification(string specificationId,
            SpecificationSummary specificationSummary)
            => _specifications.Setup(_ => _.GetSpecificationSummaryById(specificationId))
                .ReturnsAsync(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

        private void AndTheParentJob(string parentJobId)
            => _specificationConverterDataMerge.Job = new JobViewModel
            {
                Id = parentJobId
            };

        private async Task<IActionResult> WhenTheJobIsQueued(SpecificationConverterMergeRequest request)
            => await _specificationConverterDataMerge.QueueJob(request);

        public SpecificationConverterMergeRequest NewSpecificationMergeRequest(Action<SpecificationConverterMergeRequestBuilder> setUp = null)
        {
            SpecificationConverterMergeRequestBuilder specificationConverterMergeRequestBuilder = new SpecificationConverterMergeRequestBuilder();

            setUp?.Invoke(specificationConverterMergeRequestBuilder);

            return specificationConverterMergeRequestBuilder.Build();
        }

        private Reference NewReference(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();

            setUp?.Invoke(referenceBuilder);

            return referenceBuilder.Build();
        }

        private DocumentEntity<TItem> NewDocument<TItem>(TItem item)
            where TItem : IIdentifiable
            => new DocumentEntity<TItem>(item);

        private static string NewRandomString() => new RandomString();

        private DefinitionSpecificationRelationshipVersion NewDefinitionSpecificationRelationshipVersion(Action<DefinitionSpecificationRelationshipVersionBuilder> setUp = null)
        {
            DefinitionSpecificationRelationshipVersionBuilder specificationRelationshipBuilder = new DefinitionSpecificationRelationshipVersionBuilder()
                .WithSpecification(NewReference())
                .WithDatasetDefinition(NewReference())
                .WithDatasetVersion(NewDatasetRelationshipVersion())
                .WithConverterEnabled(true);

            setUp?.Invoke(specificationRelationshipBuilder);

            return specificationRelationshipBuilder.Build();
        }

        private DefinitionSpecificationRelationship NewDefinitionSpecificationRelationship(Action<DefinitionSpecificationRelationshipBuilder> setUp = null)
        {
            DefinitionSpecificationRelationshipBuilder builder = new DefinitionSpecificationRelationshipBuilder();
            setUp?.Invoke(builder);
            return builder.Build();
        }

        private DatasetRelationshipVersion NewDatasetRelationshipVersion(Action<DatasetRelationshipVersionBuilder> setUp = null)
        {
            DatasetRelationshipVersionBuilder datasetRelationshipVersionBuilder = new DatasetRelationshipVersionBuilder();

            setUp?.Invoke(datasetRelationshipVersionBuilder);

            return datasetRelationshipVersionBuilder.Build();
        }

        private SpecificationSummary NewSpecificationSummary(Action<ApiSpecificationSummaryBuilder> setUp = null)
        {
            ApiSpecificationSummaryBuilder specificationSummaryBuilder = new ApiSpecificationSummaryBuilder();

            setUp?.Invoke(specificationSummaryBuilder);

            return specificationSummaryBuilder.Build();
        }

        private FundingConfiguration NewFundingConfiguration(Action<FundingConfigurationBuilder> setUp = null)
        {
            FundingConfigurationBuilder fundingConfigurationBuilder = new FundingConfigurationBuilder();

            setUp?.Invoke(fundingConfigurationBuilder);

            return fundingConfigurationBuilder.Build();
        }
    }
}