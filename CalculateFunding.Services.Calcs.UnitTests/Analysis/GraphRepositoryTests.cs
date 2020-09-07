using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Graph;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Calcs.Analysis;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog;
using ApiSpecification = CalculateFunding.Common.ApiClient.Graph.Models.Specification;
using ApiCalculation = CalculateFunding.Common.ApiClient.Graph.Models.Calculation;
using ApiDataField = CalculateFunding.Common.ApiClient.Graph.Models.DataField;
using ApiDataSet = CalculateFunding.Common.ApiClient.Graph.Models.Dataset;
using ApiDatasetDefinition = CalculateFunding.Common.ApiClient.Graph.Models.DatasetDefinition;
using ApiEntitySpecification = CalculateFunding.Common.ApiClient.Graph.Models.Entity<CalculateFunding.Common.ApiClient.Graph.Models.Specification>;
using ApiEntityCalculation = CalculateFunding.Common.ApiClient.Graph.Models.Entity<CalculateFunding.Common.ApiClient.Graph.Models.Calculation>;
using ApiRelationship = CalculateFunding.Common.ApiClient.Graph.Models.Relationship;
using CalculateFunding.Common.ApiClient.Models;
using NSubstitute;

namespace CalculateFunding.Services.Calcs.UnitTests.Analysis
{
    [TestClass]
    public class GraphRepositoryTests : GraphTestBase
    {
        private ReIndexGraphRepository _repository;
        private Mock<IGraphApiClient> _graphApiClient;

        [TestInitialize]
        public void SetUp()
        {
            _graphApiClient = new Mock<IGraphApiClient>();

            _repository = new ReIndexGraphRepository(_graphApiClient.Object,
                new ResiliencePolicies
                {
                    GraphApiClientPolicy = Policy.NoOpAsync()
                },
                Mapper.Object,
                new Mock<ILogger>().Object);
        }

        [TestMethod]
        public void GuardsAgainstNoSpecificationCalculationRelationshipsBeingSupplied()
        {
            Func<Task> invocation = () => WhenTheGraphIsRecreated(null, null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("specificationCalculationRelationships");
        }

        [TestMethod]
        [DataRow(false, true)]
        [DataRow(true, true)]
        [DataRow(false, false)]
        public async Task DeletesThenInsertsGraphForSpecification(bool withCallsCalculation, bool withBelongsToSpecification)
        {
            string specificationId = NewRandomString();

            string calculationIdOne = NewRandomString();
            string calculationIdTwo = NewRandomString();
            string calculationIdThree = NewRandomString();
            string calculationIdFour = NewRandomString();
            string calculationIdFive = NewRandomString();
            string datafieldId = NewRandomString();

            Specification specification = NewGraphSpecification(_ => _.WithId(specificationId));

            IEnumerable<ApiEntitySpecification> existingEntities = new[] { 
                new ApiEntitySpecification {
                    Node = new ApiSpecification { SpecificationId = specificationId},
                    Relationships = withBelongsToSpecification ? new[] { 
                        new ApiRelationship { One = NewGraphCalculation(_ => _.WithId(calculationIdFive)), Type = "BelongsToSpecification", Two = specification } 
                    } : null
                }
            };

            if (withCallsCalculation)
            {
                existingEntities = existingEntities.Concat(new[] {
                    new ApiEntitySpecification
                    {
                        Node = new ApiSpecification { SpecificationId = specificationId },
                        Relationships = new[] { new ApiRelationship { One = NewGraphCalculation(_ => _.WithId(calculationIdFour)), Type = "CallsCalculation", Two = NewGraphCalculation(_ => _.WithId(calculationIdFive)) } }
                    } 
                });
            }

            IEnumerable<ApiEntityCalculation> existingCalculationEntities = new[] { new ApiEntityCalculation
            {
                Node = new ApiCalculation { CalculationId = calculationIdFive },
                Relationships = new[] { new ApiRelationship { One = NewGraphCalculation(_ => _.WithId(calculationIdOne)), Type = "ReferencesDataField", Two = NewDataField(_ => _.WithCalculationId(calculationIdOne).WithDataFieldId(datafieldId)) } } } 
            };

            Calculation[] calculations = new[]
            {
                NewGraphCalculation(_ => _.WithId(calculationIdOne)),
                NewGraphCalculation(_ => _.WithId(calculationIdTwo)),
                NewGraphCalculation(_ => _.WithId(calculationIdThree)),
                NewGraphCalculation(_ => _.WithId(calculationIdFour)),
            };
            Calculation[] unusedCalculations = new[]
            {
                NewGraphCalculation(_ => _.WithId(calculationIdFive)),
            };
            CalculationRelationship[] calculationRelationships = new[]
            {
                NewCalculationRelationship(_ => _.WithCalculationOneId(calculationIdTwo)
                    .WithCalculationTwoId(calculationIdOne)),
                NewCalculationRelationship(_ => _.WithCalculationOneId(calculationIdThree)
                    .WithCalculationTwoId(calculationIdTwo)),
                NewCalculationRelationship(_ => _.WithCalculationOneId(calculationIdFour)
                    .WithCalculationTwoId(calculationIdThree))
            };
            IEnumerable<CalculationRelationship> unusedCalculationRelationships = new CalculationRelationship[0];

            if (withCallsCalculation)
            {
                unusedCalculationRelationships = unusedCalculationRelationships.Concat(new[]
                {
                    NewCalculationRelationship(_ => _.WithCalculationOneId(calculationIdFour)
                        .WithCalculationTwoId(calculationIdFive))
                });
            }

            CalculationDataFieldRelationship[] dataFieldRelationships = calculations.Select(_ => new CalculationDataFieldRelationship
            {
                Calculation = _,
                DataField = NewDataField(datafieldBuilder =>
                    datafieldBuilder.WithCalculationId(_.CalculationId))
            }).ToArray();

            Dataset newDataset = NewDataset();

            DatasetDataFieldRelationship[] datasetDataFieldRelationships = dataFieldRelationships.Select(_ => new DatasetDataFieldRelationship
            {
                DataField = _.DataField,
                Dataset = newDataset
            }).ToArray();

            DatasetDefinition newDatasetDefinition = NewDatasetDefinition();

            DatasetDatasetDefinitionRelationship[] datasetDatasetDefinitionRelationships = datasetDataFieldRelationships.Select(_ => new DatasetDatasetDefinitionRelationship
            {
                Dataset = _.Dataset,
                DatasetDefinition = newDatasetDefinition
            }).ToArray();

            CalculationDataFieldRelationship[] unusedDataFieldRelationships = new[] {new CalculationDataFieldRelationship
            {
                Calculation = NewGraphCalculation(_ => _.WithId(calculationIdOne)),
                DataField = NewDataField(datasetBuilder =>
                    datasetBuilder.WithCalculationId(calculationIdOne).WithDataFieldId(datafieldId))
            } };

            SpecificationCalculationRelationships specificationCalculationRelationships = NewSpecificationCalculationRelationships(_ =>
                _.WithSpecification(specification)
                    .WithCalculations(calculations)
                    .WithCalculationRelationships(calculationRelationships)
                    .WithCalculationDataFieldRelationships(dataFieldRelationships)
                    .WithDatasetDataFieldRelationships(datasetDataFieldRelationships)
                    .WithDatasetDatasetDefinitionRelationships(datasetDatasetDefinitionRelationships));

            ApiSpecification apiSpecification = NewApiSpecification();
            ApiCalculation[] apiCalculations = new[]
            {
                NewApiCalculation(),
                NewApiCalculation(),
                NewApiCalculation()
            };
            
            GivenTheMapping(specification, apiSpecification);

            foreach(CalculationDataFieldRelationship datFieldRelationship in dataFieldRelationships)
            {
                GivenTheMapping(datFieldRelationship.DataField, new ApiDataField
                {
                    CalculationId = datFieldRelationship.DataField.CalculationId,
                    DataFieldId = datFieldRelationship.DataField.DataFieldId,
                    DataFieldName = datFieldRelationship.DataField.DataFieldName
                });
            }

            AndTheMapping(newDataset, new ApiDataSet
            {
                DatasetId = newDataset.DatasetId,
                Name = newDataset.Name
            });

            AndTheMapping(newDatasetDefinition, new ApiDatasetDefinition
            {
                DatasetDefinitionId = newDatasetDefinition.DatasetDefinitionId,
                Name = newDatasetDefinition.Name
            });

            AndTheCollectionMapping(calculations, apiCalculations);
            AndTheSpecificationRelationshipsAreDeleted(calculationIdFive, specificationId, unusedCalculationRelationships, unusedDataFieldRelationships, withBelongsToSpecification);
            AndTheSpecificationIsCreated(apiSpecification);
            AndTheCalculationsAreCreated(apiCalculations);
            AndTheSpecificationCalculationRelationshipsWereCreated(specificationId, new []
            {
                calculationIdOne,
                calculationIdTwo,
                calculationIdThree,
                calculationIdFour
            });
            AndTheExistingRelationships(specificationId, existingEntities);
            AndTheExistingCalculationRelationships(calculationIdOne, existingCalculationEntities);
            AndTheRelationshipsWereCreated(calculationRelationships);
            AndTheDataFieldRelationshipsWereCreated(dataFieldRelationships);
            AndTheDatasetDataFieldRelationshipsWereCreated(datasetDataFieldRelationships, specificationId);
            AndTheDatasetDatasetDefinitionRelationshipsWereCreated(datasetDatasetDefinitionRelationships);

            SpecificationCalculationRelationships specificationUnusedCalculationRelationships = await WhenTheUnusedRelationshipsAreReturned(specificationCalculationRelationships);

            await AndTheGraphIsRecreated(specificationCalculationRelationships, specificationUnusedCalculationRelationships);

            _graphApiClient.VerifyAll();
        }

        private void AndTheSpecificationRelationshipsAreDeleted(string calculationId, string specificationId, IEnumerable<CalculationRelationship> calculationRelationships, IEnumerable<CalculationDataFieldRelationship> datasetFieldRelationships, bool withBelongsToSpecification)
        {
            if (withBelongsToSpecification)
            {
                _graphApiClient.Setup(_ => _.DeleteCalculationSpecificationRelationship(calculationId, specificationId))
                    .ReturnsAsync(HttpStatusCode.OK)
                    .Verifiable();
            }

            foreach(CalculationRelationship calculationRelation in calculationRelationships)
            {
                _graphApiClient.Setup(_ => _.DeleteCalculationCalculationRelationship(calculationRelation.CalculationOneId, calculationRelation.CalculationTwoId))
                    .ReturnsAsync(HttpStatusCode.OK)
                    .Verifiable();
            }

            foreach (CalculationDataFieldRelationship datasetFieldRelationship in datasetFieldRelationships)
            {
                _graphApiClient.Setup(_ => _.DeleteCalculationDataFieldRelationship(datasetFieldRelationship.Calculation.CalculationId, datasetFieldRelationship.DataField.DataFieldId))
                    .ReturnsAsync(HttpStatusCode.OK)
                    .Verifiable();
            }
        }

        private void AndTheSpecificationIsCreated(ApiSpecification specification)
        {
            _graphApiClient.Setup(_ => _.UpsertSpecifications(It.Is<ApiSpecification[]>(specs =>
                    specs.SequenceEqual(new [] { specification }))))
                .ReturnsAsync(HttpStatusCode.OK)
                .Verifiable();
        }

        private void AndTheCalculationsAreCreated(params ApiCalculation[] calculations)
        {
            _graphApiClient.Setup(_ => _.UpsertCalculations(It.Is<ApiCalculation[]>(calcs =>
                    calculations.SequenceEqual(calcs))))
                .ReturnsAsync(HttpStatusCode.OK)
                .Verifiable();
        }

        private void AndTheExistingRelationships(string specificationId, IEnumerable<ApiEntitySpecification> entitities)
        {
            _graphApiClient.Setup(_ => _.GetAllEntitiesRelatedToSpecification(specificationId))
                .ReturnsAsync(new ApiResponse<IEnumerable<ApiEntitySpecification>>(HttpStatusCode.OK, entitities))
                .Verifiable();
        }

        private void AndTheExistingCalculationRelationships(string calculationId, IEnumerable<ApiEntityCalculation> entitities)
        {
            _graphApiClient.Setup(_ => _.GetAllEntitiesRelatedToCalculation(calculationId))
                    .ReturnsAsync(new ApiResponse<IEnumerable<ApiEntityCalculation>>(HttpStatusCode.OK, entitities))
                    .Verifiable();
        }

        private void AndTheRelationshipsWereCreated(CalculationRelationship[] calculationRelationships)
        {
            IEnumerable<IGrouping<string, CalculationRelationship>> relationshipsPerCalculation =
                calculationRelationships.GroupBy(_ => _.CalculationOneId);

            foreach (IGrouping<string, CalculationRelationship> relationships in relationshipsPerCalculation)
            {
                _graphApiClient.Setup(_ => _.UpsertCalculationCalculationsRelationships(relationships.Key, It.Is<string[]>(calcs =>
                        calcs.SequenceEqual(relationships.Select(rel => rel.CalculationTwoId).ToArray()))))
                    .ReturnsAsync(HttpStatusCode.OK)
                    .Verifiable();
            }
        }

        private void AndTheDataFieldRelationshipsWereCreated(CalculationDataFieldRelationship[] datafieldRelationships)
        {
            IEnumerable<IGrouping<string, CalculationDataFieldRelationship>> relationshipsPerCalculation =
                datafieldRelationships.GroupBy(_ => _.Calculation.CalculationId);

            foreach (IGrouping<string, CalculationDataFieldRelationship> relationships in relationshipsPerCalculation)
            {
                _graphApiClient.Setup(_ => _.UpsertDataFields(It.Is<ApiDataField[]>(datasetFields =>
                        datasetFields.Select(_ => _.DataFieldId).SequenceEqual(relationships.Select(rel => rel.DataField.DataFieldId).Distinct().ToArray()))))
                    .ReturnsAsync(HttpStatusCode.OK)
                    .Verifiable();

                _graphApiClient.Setup(_ => _.UpsertCalculationDataFieldsRelationships(relationships.Key, It.Is<string[]>(calcs =>
                        calcs.SequenceEqual(relationships.Select(rel => rel.DataField.DataFieldId).ToArray()))))
                    .ReturnsAsync(HttpStatusCode.OK)
                    .Verifiable();
            }
        }

        private void AndTheDatasetDataFieldRelationshipsWereCreated(DatasetDataFieldRelationship[] datasetDataFieldRelationship, string specificationId)
        {
            IEnumerable<IGrouping<string, DatasetDataFieldRelationship>> relationshipsPerDataset =
                datasetDataFieldRelationship.GroupBy(_ => _.Dataset.DatasetId);

            foreach (IGrouping<string, DatasetDataFieldRelationship> relationships in relationshipsPerDataset)
            {
                _graphApiClient.Setup(_ => _.UpsertDataset(It.Is<ApiDataSet>(dataset =>
                        dataset.DatasetId == relationships.Select(rel => rel.Dataset.DatasetId).Distinct().First())))
                    .ReturnsAsync(HttpStatusCode.OK)
                    .Verifiable();

                _graphApiClient.Setup(_ => _.UpsertSpecificationDatasetRelationship(specificationId, relationships.Key))
                    .ReturnsAsync(HttpStatusCode.OK)
                    .Verifiable();

                foreach (string fieldid in relationships.Select(_ => _.DataField.DataFieldId))
                {
                    _graphApiClient.Setup(_ => _.UpsertDatasetDataFieldRelationship(relationships.Key, fieldid))
                    .ReturnsAsync(HttpStatusCode.OK)
                    .Verifiable();
                }
            }
        }

        private void AndTheDatasetDatasetDefinitionRelationshipsWereCreated(DatasetDatasetDefinitionRelationship[] datasetDatasetDefinitionRelationships)
        {
            IEnumerable<IGrouping<string, DatasetDatasetDefinitionRelationship>> relationshipsPerDataset =
                datasetDatasetDefinitionRelationships.GroupBy(_ => _.Dataset.DatasetId);

            foreach (IGrouping<string, DatasetDatasetDefinitionRelationship> relationships in relationshipsPerDataset)
            {
                _graphApiClient.Setup(_ => _.UpsertDatasetDefinitions(It.Is<ApiDatasetDefinition[]>(datasetDefinitions =>
                        datasetDefinitions.Select(_ => _.DatasetDefinitionId).SequenceEqual(relationships.Select(rel => rel.DatasetDefinition.DatasetDefinitionId).Distinct().ToArray()))))
                    .ReturnsAsync(HttpStatusCode.OK)
                    .Verifiable();

                foreach (string definitionid in relationships.Select(_ => _.DatasetDefinition.DatasetDefinitionId))
                {
                    _graphApiClient.Setup(_ => _.UpsertDataDefinitionDatasetRelationship(definitionid,
                            relationships.Key))
                    .ReturnsAsync(HttpStatusCode.OK)
                    .Verifiable();
                }
            }
        }

        private void AndTheSpecificationCalculationRelationshipsWereCreated(string specificationId, string[] calculationIds)
        {
            foreach (string calculationId in calculationIds)
            {
                _graphApiClient.Setup(_ => _.UpsertCalculationSpecificationRelationship(calculationId, specificationId))
                    .ReturnsAsync(HttpStatusCode.OK)
                    .Verifiable();
            }
        }

        private async Task<SpecificationCalculationRelationships> WhenTheUnusedRelationshipsAreReturned(SpecificationCalculationRelationships specificationCalculationRelationships)
        {
            return await _repository.GetUnusedRelationships(specificationCalculationRelationships);
        }

        private async Task AndTheGraphIsRecreated(SpecificationCalculationRelationships specificationCalculationRelationships, SpecificationCalculationRelationships specificationUnusedCalculationRelationships)
        {
            await WhenTheGraphIsRecreated(specificationCalculationRelationships, specificationUnusedCalculationRelationships);
        }

        private async Task WhenTheGraphIsRecreated(SpecificationCalculationRelationships specificationCalculationRelationships, SpecificationCalculationRelationships specificationUnusedCalculationRelationships)
        {
            await _repository.RecreateGraph(specificationCalculationRelationships, specificationUnusedCalculationRelationships);
        }

        private ApiSpecification NewApiSpecification()
        {
            return new GraphApiSpecificationBuilder()
                .Build();
        }

        private ApiCalculation NewApiCalculation()
        {
            return new GraphApiCalculationBuilder()
                .Build();
        }
    }
}