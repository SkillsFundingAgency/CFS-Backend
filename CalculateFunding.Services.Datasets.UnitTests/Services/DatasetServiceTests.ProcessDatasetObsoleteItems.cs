using CalculateFunding.Common.ApiClient.Calcs.Models.ObsoleteItems;
using CalculateFunding.Common.ApiClient.Graph;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GraphCalculation = CalculateFunding.Common.ApiClient.Graph.Models.Calculation;
using GraphEntityCalculation = CalculateFunding.Common.ApiClient.Graph.Models.Entity<CalculateFunding.Common.ApiClient.Graph.Models.Calculation>;
using GraphEntityFundingLine = CalculateFunding.Common.ApiClient.Graph.Models.Entity<CalculateFunding.Common.ApiClient.Graph.Models.FundingLine>;
using CalculationRelationship = CalculateFunding.Models.Graph.CalculationRelationship;
using FundingLineCalculationRelationship = CalculateFunding.Models.Graph.FundingLineCalculationRelationship;
using Relationship = CalculateFunding.Common.ApiClient.Graph.Models.Relationship;
using static CalculateFunding.Tests.Common.Helpers.ConstraintHelpers;
using CalculateFunding.Models.Calcs;

namespace CalculateFunding.Services.Datasets.Services
{
    [TestClass]
    public class DatasetServiceTestsProcessDatasetObsoleteItems : DatasetServiceTestsBase
    {
        private DatasetService _datasetService;
        private Mock<ISpecificationsApiClient> _specificationsApiClient;
        private Mock<IDatasetRepository> _datasetRepository;
        private Mock<IPolicyRepository> _policyRepository;
        private Mock<ICalcsRepository> _calcsRepository;
        private Mock<IGraphApiClient> _graph;
        private Message _message;
        private string _specificationId;
        private string _specificationName;
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private uint[] _calculationIds;
        private uint[] _fundingLineIds;
        private const string TemplateVersion = "1.0";

        [TestInitialize]
        public void SetUp()
        {
            _specificationsApiClient = CreateSpecificationsApiClient();
            _datasetRepository = CreateMockDatasetsRepository();
            _policyRepository = CreateMockPolicyRepository();
            _calcsRepository = CreateMockCalcsRepository();
            _graph = CreateMockGraphApiClient();

            _specificationId = NewRandomString();
            _specificationName = "SpecificationName";
            _fundingStreamId = NewRandomString();
            _fundingPeriodId = NewRandomString();

            _calculationIds = new[] { NewRandomUint() };
            _fundingLineIds = new[] { NewRandomUint() };

            _datasetService = CreateDatasetService(specificationsApiClient: _specificationsApiClient.Object,
                                                datasetRepository: _datasetRepository.Object,
                                                policyRepository: _policyRepository.Object,
                                                calcsRepository: _calcsRepository.Object,
                                                graphApiClient: _graph.Object);

            _message = new Message();
        }

        [TestMethod]
        public async Task ProcessDatasetObsoleteItems_GivenValidMessageWithObsoleteItems_ObsoleteItemsProcessed()
        {
            GivenMessageWithSpecificationId();
            AndSpecification();
            string relationshipName = "Relationship";

            DefinitionSpecificationRelationship relationshipOne = NewDefinitionSpecificationRelationship(r => r.WithName(relationshipName)
                                    .WithCurrent(
                                    NewDefinitionSpecificationRelationshipVersion(rv => rv.WithSpecification(new Reference { Id = _specificationId })
                                        .WithPublishedSpecificationConfiguration(
                                            new PublishedSpecificationConfiguration
                                            {
                                                Calculations = _calculationIds.Select(_ => new PublishedSpecificationItem { TemplateId = _, SourceCodeName = _.ToString() }),
                                                FundingLines = _fundingLineIds.Select(_ => new PublishedSpecificationItem { TemplateId = _, SourceCodeName = _.ToString() })
                                            }
                                    ))
                                ));
            DefinitionSpecificationRelationship relationshipTwo = NewDefinitionSpecificationRelationship(r => r.WithCurrent(NewDefinitionSpecificationRelationshipVersion()));
            DefinitionSpecificationRelationship relationshipThree = NewDefinitionSpecificationRelationship(r => r.WithCurrent(NewDefinitionSpecificationRelationshipVersion()));
            
            AndDefinitionSpecificationRelationships(relationshipOne, relationshipTwo, relationshipThree);
            AndTheTemplateContents();

            string calculationReferencingReleasedDataFundingLine = Guid.NewGuid().ToString();
            string obsoleteFundingLineItemId = Guid.NewGuid().ToString();
            AndTheCalculationReferencesObsoleteFundingLine(calculationReferencingReleasedDataFundingLine, $"Datasets.{relationshipName}.{CodeGenerationDatasetTypeConstants.FundingLinePrefix}_{_fundingLineIds.First()}_{_fundingLineIds.First()}");
            AndTheObsoleteItemsAreCreatedSuccessfully(obsoleteFundingLineItemId, _fundingLineIds.First().ToString());
            AndCalculationAddedToObsoleteItem(obsoleteFundingLineItemId, calculationReferencingReleasedDataFundingLine);

            string obsoleteCalculationItemId = Guid.NewGuid().ToString();
            string calculationReferencingReleasedDataCalculation = Guid.NewGuid().ToString();
            AndTheCalculationReferencesObsoleteCalculation(calculationReferencingReleasedDataCalculation, $"Datasets.{relationshipName}.{CodeGenerationDatasetTypeConstants.CalculationPrefix}_{_calculationIds.First()}_{_calculationIds.First()}");
            AndTheObsoleteItemsAreCreatedSuccessfully(obsoleteCalculationItemId, _calculationIds.First().ToString());
            AndCalculationAddedToObsoleteItem(obsoleteCalculationItemId, calculationReferencingReleasedDataCalculation);

            AndDatasetRelationshipsSavedSuccessfully();
            await WhenProcessDatasetObsoleteItems();
            ThenObsoleteItemsSaved(relationshipName);
        }

        [TestMethod]
        public void ProcessDatasetObsoleteItems_GivenInvalidMessage_ExceptionThrown()
        {
            Func<Task> invocation = async() => await WhenProcessDatasetObsoleteItems();

            invocation
                .Should()
                .ThrowAsync<NonRetriableException>()
                .Result
                .WithMessage("Null or empty specification Id provided for process dataset obsolete items");
        }

        [TestMethod]
        public void ProcessDatasetObsoleteItems_GivenValidMessageButSpecificationFailedToRetrieve_ExceptionThrown()
        {
            GivenMessageWithSpecificationId();

            Func<Task> invocation = async() => await WhenProcessDatasetObsoleteItems();

            invocation
                .Should()
                .ThrowAsync<RetriableException>()
                .Result
                .WithMessage($"Failed to fetch specification summary for specification ID: {_specificationId}");
        }

        [TestMethod]
        public void ProcessDatasetObsoleteItems_GivenValidMessageWithObsoleteItemsButSaveDefinitionRelationshipFails_ExceptionThrown()
        {
            GivenMessageWithSpecificationId();
            AndSpecification();

            DefinitionSpecificationRelationship relationshipOne = NewDefinitionSpecificationRelationship(r => r.WithCurrent(
                                    NewDefinitionSpecificationRelationshipVersion(rv => rv.WithSpecification(new Reference { Id = _specificationId })
                                        .WithPublishedSpecificationConfiguration(
                                            new PublishedSpecificationConfiguration
                                            {
                                                Calculations = _calculationIds.Select(_ => new PublishedSpecificationItem { TemplateId = _, IsObsolete = true }),
                                                FundingLines = _fundingLineIds.Select(_ => new PublishedSpecificationItem { TemplateId = _ })
                                            }
                                    ))
                                ));
            DefinitionSpecificationRelationship relationshipTwo = NewDefinitionSpecificationRelationship(r => r.WithCurrent(NewDefinitionSpecificationRelationshipVersion()));
            DefinitionSpecificationRelationship relationshipThree = NewDefinitionSpecificationRelationship(r => r.WithCurrent(NewDefinitionSpecificationRelationshipVersion()));

            AndDefinitionSpecificationRelationships(relationshipOne, relationshipTwo, relationshipThree);
            AndTheTemplateContents();
            AndDatasetRelationshipsSaveUnsuccessful();
            Func<Task> invocation = async() => await WhenProcessDatasetObsoleteItems();

            invocation
                .Should()
                .ThrowAsync<RetriableException>()
                .Result
                .WithMessage($"Failed to save definition specification relationship for obsolete items for relationship:{relationshipOne.Id}.");
        }

        private async Task WhenProcessDatasetObsoleteItems()
        {
            await _datasetService.ProcessDatasetObsoleteItems(_message);
        }

        private void AndTheObsoleteItemsAreCreatedSuccessfully(string obsoleteItem, string datasetFieldId)
        {
            _calcsRepository.Setup(_ => _.CreateObsoleteItem(It.Is<ObsoleteItem>(obsoleteItem =>
                    obsoleteItem.SpecificationId == _specificationId &&
                    obsoleteItem.IsReleasedData == true &&
                    obsoleteItem.DatasetFieldId == datasetFieldId)))
            .ReturnsAsync(new ObsoleteItem
            {
                Id = obsoleteItem
            });
        }

        private void ThenObsoleteItemsSaved(string relationshipName)
        {
            _calcsRepository.Verify(_ => _.CreateObsoleteItem(It.Is<ObsoleteItem>(obsoleteItem => 
                    obsoleteItem.SpecificationId == _specificationId &&
                    obsoleteItem.IsReleasedData == true &&
                    obsoleteItem.CodeReference == $"Datasets.{relationshipName}.{CodeGenerationDatasetTypeConstants.FundingLinePrefix}_{_fundingLineIds.First()}_{_fundingLineIds.First()}" &&
                    obsoleteItem.DatasetFieldId == _fundingLineIds.First().ToString())), Times.Once);

            _calcsRepository.Verify(_ => _.CreateObsoleteItem(It.Is<ObsoleteItem>(obsoleteItem =>
                    obsoleteItem.SpecificationId == _specificationId &&
                    obsoleteItem.IsReleasedData == true &&
                    obsoleteItem.CodeReference == $"Datasets.{relationshipName}.{CodeGenerationDatasetTypeConstants.CalculationPrefix}_{_calculationIds.First()}_{_calculationIds.First()}" &&
                    obsoleteItem.DatasetFieldId == _calculationIds.First().ToString())), Times.Once);
        }

        private void AndCalculationAddedToObsoleteItem(string obsoleteItemId, string calculationId)
        {
            _calcsRepository.Setup(_ => _.AddCalculationToObsoleteItem(obsoleteItemId, calculationId))
                .ReturnsAsync(HttpStatusCode.OK);
        }

        private void AndDatasetRelationshipsSavedSuccessfully()
        {
            _datasetRepository.Setup(_ => _.SaveDefinitionSpecificationRelationship(It.Is<DefinitionSpecificationRelationship>(r =>
                            r.Current.Specification.Id == _specificationId)))
                .ReturnsAsync(HttpStatusCode.OK);
        }
        private void AndDatasetRelationshipsSaveUnsuccessful()
        {
            _datasetRepository.Setup(_ => _.SaveDefinitionSpecificationRelationship(It.Is<DefinitionSpecificationRelationship>(r =>
                            r.Current.Specification.Id == _specificationId)))
                .ReturnsAsync(HttpStatusCode.InternalServerError);
        }

        private void GivenMessageWithSpecificationId()
        {
            _message.UserProperties["specification-id"] = _specificationId;
        }

        private Mock<ISpecificationsApiClient> CreateSpecificationsApiClient()
        {
            return new Mock<ISpecificationsApiClient>();
        }

        private Mock<IDatasetRepository> CreateMockDatasetsRepository()
        {
            return new Mock<IDatasetRepository>();
        }

        private Mock<IPolicyRepository> CreateMockPolicyRepository()
        {
            return new Mock<IPolicyRepository>();
        }

        private Mock<ICalcsRepository> CreateMockCalcsRepository()
        {
            return new Mock<ICalcsRepository>();
        }

        private Mock<IGraphApiClient> CreateMockGraphApiClient()
        {
            return new Mock<IGraphApiClient>();
        }

        private void AndSpecification()
        {
            _specificationsApiClient.Setup(_ => _.GetSpecificationSummaryById(_specificationId))
                .ReturnsAsync(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK,
                    NewSpecificationSummary(_ => _.WithId(_specificationId)
                                                    .WithName(_specificationName)
                                                    .WithFundingStreamIds(new[] { _fundingStreamId })
                                                    .WithFundingPeriodId(_fundingPeriodId)
                                                    .WithTemplateVersions(( _fundingStreamId, TemplateVersion ))),
                    null));
        }

        private void AndDefinitionSpecificationRelationships(params DefinitionSpecificationRelationship[] relationships)
        {
            _datasetRepository.Setup(_ => _.GetDefinitionSpecificationRelationshipsByQuery(It.Is<Expression<Func<DocumentEntity<DefinitionSpecificationRelationship>, bool>>>(query
                    => BooleanExpressionMatches(query,
                        NewDocument(NewDefinitionSpecificationRelationship(r => r.WithCurrent(NewDefinitionSpecificationRelationshipVersion(relationship
                            => relationship.WithRelationshipType(DatasetRelationshipType.ReleasedData
                                )
                                .WithPublishedSpecificationConfiguration(new PublishedSpecificationConfiguration { SpecificationId = _specificationId })))))))))
                .ReturnsAsync(relationships);
        }

        private void AndTheTemplateContents()
        {
            _policyRepository.Setup(_ => _.GetDistinctTemplateMetadataContents(_fundingStreamId,
                _fundingPeriodId,
                TemplateVersion))
            .ReturnsAsync(new TemplateMetadataDistinctContents());
        }

        private void AndTheCalculationReferencesObsoleteFundingLine(string calculationId, string fundingLineSourceName)
        {
            _graph.Setup(_ => _.GetAllEntitiesRelatedToFundingLine(fundingLineSourceName))
                .ReturnsAsync(new ApiResponse<IEnumerable<GraphEntityFundingLine>>(HttpStatusCode.OK,
                    new[] { 
                        new GraphEntityFundingLine { 
                            Relationships = new[] { 
                                new Relationship {
                                    One = new GraphCalculation {
                                        CalculationId = calculationId
                                    },
                                    Type = FundingLineCalculationRelationship.ToIdField
                                } 
                            }
                        } 
                    }));
        }

        private void AndTheCalculationReferencesObsoleteCalculation(string calculationId, string calculationSourceName)
        {
            _graph.Setup(_ => _.GetAllEntitiesRelatedToCalculation(calculationSourceName))
                .ReturnsAsync(new ApiResponse<IEnumerable<GraphEntityCalculation>>(HttpStatusCode.OK,
                    new[] {
                        new GraphEntityCalculation {
                            Relationships = new[] {
                                new Relationship {
                                    One = new GraphCalculation {
                                        CalculationId = calculationId
                                    },
                                    Type = CalculationRelationship.ToIdField
                                }
                            }
                        }
                    }));
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


        static string NewRandomString() => new RandomString();
        static uint NewRandomUint() => (uint)new RandomNumberBetween(0, int.MaxValue);
    }
}
