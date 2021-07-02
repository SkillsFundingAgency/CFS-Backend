﻿using AutoFixture;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.CodeGeneration.VisualBasic.UnitTests
{
    public class DatasetRelationshipSummaryBuilder
    {
        private DatasetRelationshipSummary datasetRelationshipSummary;

        public DatasetRelationshipSummaryBuilder()
        {
            Fixture fixture = new Fixture();
            datasetRelationshipSummary = fixture.Create<DatasetRelationshipSummary>();
        }

        public DatasetRelationshipSummaryBuilder WithType(DatasetRelationshipType datasetRelationshipType)
        {
            datasetRelationshipSummary.RelationshipType = datasetRelationshipType;
            return this;
        }

        public DatasetRelationshipSummaryBuilder WithTableDefinitions(IEnumerable<TableDefinition> tableDefinitions)
        {
            datasetRelationshipSummary.DatasetDefinition.TableDefinitions = tableDefinitions.ToList();
            return this;
        }

        public DatasetRelationshipSummaryBuilder WithPublishedSpecificationConfigurationFundingLines(
            IEnumerable<PublishedSpecificationItem> publishedSpecificationItems)
        {
            datasetRelationshipSummary.PublishedSpecificationConfiguration.FundingLines = publishedSpecificationItems;
            return this;
        }

        public DatasetRelationshipSummaryBuilder WithPublishedSpecificationConfigurationCalculations(
            IEnumerable<PublishedSpecificationItem> publishedSpecificationItems)
        {
            datasetRelationshipSummary.PublishedSpecificationConfiguration.Calculations = publishedSpecificationItems;
            return this;
        }

        public DatasetRelationshipSummary Build()
        {
            return datasetRelationshipSummary;
        }
    }
}
