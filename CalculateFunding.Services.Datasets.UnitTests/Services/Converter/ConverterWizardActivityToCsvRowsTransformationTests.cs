using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Services.Datasets.Converter;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Datasets.Services.Converter
{
    [TestClass]
    public class ConverterWizardActivityToCsvRowsTransformationTests
    {
        private ConverterWizardActivityToCsvRowsTransformation _transformation;

        [TestInitialize]
        public void SetUp()
        {
            _transformation = new ConverterWizardActivityToCsvRowsTransformation();
        }

        [TestMethod]
        public void FlattenProviderConvertersAndConverterDataMergeLogsIntoRows()
        {
            string previousProviderIdentifierOne = new RandomString();
            string targetProviderNameOne = new RandomString();
            string targetProviderIdOne = new RandomString();
            string targetStatusOne = new RandomString();

            string previousProviderIdentifierTwo = new RandomString();
            string targetProviderNameTwo = new RandomString();
            string targetProviderIdTwo = new RandomString();
            string targetStatusTwo = new RandomString();

            string previousProviderIdentifierThree = new RandomString();
            string targetProviderNameThree = new RandomString();
            string targetProviderIdThree = new RandomString();
            string targetStatusThree = new RandomString();

            string datasetRelationshipId = new RandomString();
            string datasetRelationshipName = new RandomString();

            IEnumerable<ProviderConverterDetail> providerConverterDetails = NewProviderConverterDetails(_ => _.WithPreviousProviderIdentifier(previousProviderIdentifierOne)
                .WithTargetProviderName(targetProviderNameOne)
                .WithTargetProviderId(targetProviderIdOne)
                .WithTargetStatus(targetStatusOne),
                _ => _.WithPreviousProviderIdentifier(previousProviderIdentifierTwo)
                .WithTargetProviderName(targetProviderNameTwo)
                .WithTargetProviderId(targetProviderIdTwo)
                .WithTargetStatus(targetStatusTwo),
                _ => _.WithPreviousProviderIdentifier(previousProviderIdentifierThree)
                .WithTargetProviderName(targetProviderNameThree)
                .WithTargetProviderId(targetProviderIdThree)
                .WithTargetStatus(targetStatusThree)
                .WithProviderInEligible("Multiple predecessors"));

            IEnumerable<ConverterDataMergeLog> converterDataMergeLogs =
                NewConverterDataMergeLogs(_ => _.WithRequest(
                    NewConverterMergeRequest(
                        cmr => cmr.WithDatasetRelationshipId(datasetRelationshipId)))
                        .WithResults(
                            NewRowCopyResults(
                                rcr => rcr.WithEligibleConverter(
                                    NewProviderConverter(
                                        pcr => pcr.WithPreviousProviderIdentifier(previousProviderIdentifierOne)
                                                .WithTargetProviderId(targetProviderIdOne)))
                                                .WithOutcome(RowCopyOutcome.Copied),
                                rcr => rcr.WithEligibleConverter(
                                    NewProviderConverter(
                                        pcr => pcr.WithPreviousProviderIdentifier(previousProviderIdentifierTwo)
                                                .WithTargetProviderId(targetProviderIdTwo)))
                                                .WithOutcome(RowCopyOutcome.ValidationFailure)
                                )
                        )
                );

            IEnumerable<DatasetSpecificationRelationshipViewModel> datasetSpecificationRelationshipViewModels = new[]
            {
                new DatasetSpecificationRelationshipViewModel
                {
                    ConverterWizard = true,
                    Id = datasetRelationshipId,
                    Name = datasetRelationshipName
                }
            };

            dynamic[] expectedCsvRows = {
                new Dictionary<string, object>
                {
                    { "Target UKPRN", targetProviderIdOne},
                    { "Target Provider Name", targetProviderNameOne},
                    { "Target Provider Status", targetStatusOne},
                    { "Target Opening Date", null},
                    { "Target Provider Ineligible", null},
                    { "Source Provider UKPRN", previousProviderIdentifierOne},
                    { datasetRelationshipName, RowCopyOutcome.Copied.ToString() }
                },
                new Dictionary<string, object>
                {
                    { "Target UKPRN", targetProviderIdTwo},
                    { "Target Provider Name", targetProviderNameTwo},
                    { "Target Provider Status", targetStatusTwo},
                    { "Target Opening Date", null},
                    { "Target Provider Ineligible", null},
                    { "Source Provider UKPRN", previousProviderIdentifierTwo},
                    { datasetRelationshipName, RowCopyOutcome.ValidationFailure.ToString() }
                },
                new Dictionary<string, object>
                {
                    { "Target UKPRN", targetProviderIdThree},
                    { "Target Provider Name", targetProviderNameThree},
                    { "Target Provider Status", targetStatusThree},
                    { "Target Opening Date", null},
                    { "Target Provider Ineligible", "Multiple predecessors"},
                    { "Source Provider UKPRN", previousProviderIdentifierThree},
                    { datasetRelationshipName, "Not eligible" }
                }
            };

            ExpandoObject[] transformProviderResultsIntoCsvRows = _transformation.TransformConvertWizardActivityIntoCsvRows(providerConverterDetails, converterDataMergeLogs, datasetSpecificationRelationshipViewModels).ToArray();

            transformProviderResultsIntoCsvRows
                .Should()
                .BeEquivalentTo(expectedCsvRows,
                    cfg => cfg.WithStrictOrdering());
        }

        private static IEnumerable<ProviderConverterDetail> NewProviderConverterDetails(params Action<ProviderConverterDetailBuilder>[] setUps)
        {
            return setUps.Select(NewProviderConverterDetail);
        }

        private static ProviderConverterDetail NewProviderConverterDetail(Action<ProviderConverterDetailBuilder> setUp = null)
        {
            ProviderConverterDetailBuilder providerConverterDetailBuilder = new ProviderConverterDetailBuilder();

            setUp?.Invoke(providerConverterDetailBuilder);

            return providerConverterDetailBuilder.Build();
        }

        private static ProviderConverter NewProviderConverter(Action<ProviderConverterBuilder> setUp = null)
        {
            ProviderConverterBuilder providerConverterBuilder = new ProviderConverterBuilder();

            setUp?.Invoke(providerConverterBuilder);

            return providerConverterBuilder.Build();
        }

        private static IEnumerable<ConverterDataMergeLog> NewConverterDataMergeLogs(params Action<ConverterDataMergeLogBuilder>[] setUps)
        {
            return setUps.Select(NewConverterDataMergeLog);
        }

        private static ConverterDataMergeLog NewConverterDataMergeLog(Action<ConverterDataMergeLogBuilder> setUp = null)
        {
            ConverterDataMergeLogBuilder converterDataMergeLogBuilder = new ConverterDataMergeLogBuilder();

            setUp?.Invoke(converterDataMergeLogBuilder);

            return converterDataMergeLogBuilder.Build();
        }

        private static IEnumerable<RowCopyResult> NewRowCopyResults(params Action<RowCopyResultBuilder>[] setUps)
        {
            return setUps.Select(NewRowCopyResult);
        }

        private static RowCopyResult NewRowCopyResult(Action<RowCopyResultBuilder> setUp = null)
        {
            RowCopyResultBuilder rowCopyResultBuilder = new RowCopyResultBuilder();

            setUp?.Invoke(rowCopyResultBuilder);

            return rowCopyResultBuilder.Build();
        }

        private static ConverterMergeRequest NewConverterMergeRequest(Action<ConverterMergeRequestBuilder> setUp = null)
        {
            ConverterMergeRequestBuilder converterMergeRequestBuilder = new ConverterMergeRequestBuilder();

            setUp?.Invoke(converterMergeRequestBuilder);

            return converterMergeRequestBuilder.Build();
        }

    }
}