using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CalculationType = CalculateFunding.Models.Calcs.CalculationType;
using DatasetReference = CalculateFunding.Models.Graph.DatasetReference;
using Calculation = CalculateFunding.Models.Calcs.Calculation;
using DatasetRelationshipSummary = CalculateFunding.Models.Calcs.DatasetRelationshipSummary;
using DataField = CalculateFunding.Models.Graph.DataField;
using NSubstitute;
using Serilog;
using FluentAssertions;
using CalculateFunding.Common.Models;
using System.Linq;

namespace CalculateFunding.Services.Calcs.UnitTests.Services
{
    [TestClass]
    public class DatasetReferenceServiceTests
    {
        ILogger _logger;
        DatasetReferenceService _service;

        [TestInitialize]
        public void SetUp()
        {
            _logger = Substitute.For<ILogger>();
            _service = new DatasetReferenceService(_logger);
        }

        [TestMethod]
        public void GetGetDatasetRelationShips_WhenGivenAdditionalCalculationsWithDatasetFieldReferences_ReturnDatasetReferences()
        {
            List<Calculation> calculations = CreatCalculations();

            List<DatasetRelationshipSummary> datasetRelationshipSummaries = CreatDatasetRelationshipSummary();

            List<DatasetReference> datasetReferences = _service.GetDatasetRelationShips(calculations, datasetRelationshipSummaries).ToList();

            datasetReferences.Should().NotBeEmpty();
            datasetReferences.Count.Should().Be(2);
        }

        [TestMethod]
        public void GetGetDatasetRelationShips_WhenGivenAdditionalCalculationsWithNoDatasetRelationshipSummary_ReturnNoDatasetReferences()
        {
            string specificationId = Guid.NewGuid().ToString();

            List<Calculation> calculations = CreatCalculations();

            List<DatasetReference> datasetReferences = _service.GetDatasetRelationShips(calculations, null).ToList();
            
            datasetReferences.Count.Should().Be(0);
        }

        [TestMethod]
        public void GetGetDatasetRelationShips_WhenGivenAdditionalCalculationsWithNoDatasetReferenceInCalculation_ReturnNoDatasetReferences()
        {
            string specificationId = Guid.NewGuid().ToString();

            List<Calculation> calculations = CreatCalculations();
            calculations[0].Current.SourceCode = "return 1";
            calculations[1].Current.SourceCode = "return 2";

            List<DatasetRelationshipSummary> datasetRelationshipSummaries = CreatDatasetRelationshipSummary();

            List<DatasetReference> datasetReferences = _service.GetDatasetRelationShips(calculations, datasetRelationshipSummaries).ToList();

            datasetReferences.Should().BeEmpty();
            datasetReferences.Count.Should().Be(0);
        }

        [TestMethod]
        public void GetGetDatasetRelationShips_WhenGivenAdditionalCalculationsWithInvalidDatasetFieldReference_ReturnNoDatasetReferences()
        {
            string specificationId = Guid.NewGuid().ToString();

            List<Calculation> calculations = CreatCalculations();

            List<DatasetRelationshipSummary> datasetRelationshipSummaries = CreatDatasetRelationshipSummary();
            datasetRelationshipSummaries[0].Name = "Invalid";

            List<DatasetReference> datasetReferences = _service.GetDatasetRelationShips(calculations, datasetRelationshipSummaries).ToList();

            datasetReferences.Should().NotBeEmpty();
            datasetReferences.Count.Should().Be(1);
        }


        private static List<Calculation> CreatCalculations()
        {
            return new List<Calculation>()
            {
                new Calculation() {
                    FundingStreamId="Additional",
                    SpecificationId = Guid.NewGuid().ToString(),
                    Current = new Models.Calcs.CalculationVersion(){
                        CalculationType = CalculationType.Additional,
                        SourceCode = "Dim Pupils as decimal\r\nDim Primary as decimal" +
                        "\r\n\r\nPrimary = DSG.PrimaryPupilNumber().value * DSG.PrimaryUnitOfFunding().value\r\n" +
                        "Pupils = Datasets.JBTEST202003131731.APUniversalEntitlement2YO " +
                        "\r\n\r\nReturn Math.Round(Pupils,2)",
                        CalculationId = "Calc-Id-1",
                        Name = "Calc-Name-1",

                    } },
                new Calculation() {
                    FundingStreamId="Additional",
                    SpecificationId = Guid.NewGuid().ToString(),
                    Current = new Models.Calcs.CalculationVersion(){
                        CalculationType = CalculationType.Additional,
                        SourceCode = "Dim Pupils as decimal\r\nDim Primary as decimal\r\nDim HasValue as bool" +
                        "\r\n\r\nPrimary = DSG.PrimaryPupilNumber().value * DSG.PrimaryUnitOfFunding().value\r\n" +
                        "HasValue = Datasets.JBTEST202003131731.HasValue()\r\n" +
                        "Pupils = Datasets.JBTEST202003131731.APUniversalEntitlement2YO + Datasets.JBTEST202003131734.APPupilPremium3YO " +
                        "\r\n\r\nReturn Math.Round(Pupils,2)",
                        CalculationId = "Calc-Id2",
                        Name = "Calc-Name-2",

                    } }
            };
        }

        private static List<DatasetRelationshipSummary> CreatDatasetRelationshipSummary()
        {
            return new List<DatasetRelationshipSummary>()
            {
                new DatasetRelationshipSummary()
                {
                    DatasetId = "JB TEST 202003131731",
                    Name = "JB TEST 202003131731",
                    Relationship = new Reference(){ Name = "JB TEST 202003131731"},
                    DatasetDefinition = new Models.Datasets.Schema.DatasetDefinition()
                    {
                        Name = "Early Years AP Census Year 1",
                        TableDefinitions = new List<Models.Datasets.Schema.TableDefinition>()
                        {
                            new Models.Datasets.Schema.TableDefinition()
                            {
                                Name = "Early Years AP Census Year 1",
                                FieldDefinitions = new List<Models.Datasets.Schema.FieldDefinition>()
                                {
                                    new Models.Datasets.Schema.FieldDefinition()
                                    { 
                                        Name = "AP Universal Entitlement 2YO",
                                    },
                                    new Models.Datasets.Schema.FieldDefinition()
                                    {
                                        Name = "AP Universal Entitlement 3YO",
                                    }
                                }
                            }
                        }
                    }

                },
                new DatasetRelationshipSummary()
                {
                    DatasetId = "JB TEST 202003131734",
                    Name = "JB TEST 202003131734",
                    Relationship = new Reference(){ Name = "JB TEST 202003131734"},
                    DatasetDefinition = new Models.Datasets.Schema.DatasetDefinition()
                    {
                        Name = "Early Years AP Census Year 2",
                        TableDefinitions = new List<Models.Datasets.Schema.TableDefinition>()
                        {
                            new Models.Datasets.Schema.TableDefinition()
                            {
                                Name = "Early Years AP Census Year 2",
                                FieldDefinitions = new List<Models.Datasets.Schema.FieldDefinition>()
                                {
                                    new Models.Datasets.Schema.FieldDefinition()
                                    {
                                        Name = "AP Pupil Premium 3YO",
                                    }
                                }
                            }
                        }
                    }

                }
            };
        }
    }
}
