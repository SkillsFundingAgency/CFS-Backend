using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Reporting;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using CalculateFunding.Services.Publishing.UnitTests.Reporting.FundingLines;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting
{
    [TestClass]
    public class PublishedFundingVersionFundingLineGroupingCsvTransformTests : FundingLineCsvTransformTestBase
    {
        private PublishedFundingVersionFundingLineGroupingCsvTransform _transformation;

        [TestInitialize]
        public void SetUp()
        {
            _transformation = new PublishedFundingVersionFundingLineGroupingCsvTransform();
        }

        [TestMethod]
        [DataRow(FundingLineCsvGeneratorJobType.Undefined, false)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentState, false)]
        [DataRow(FundingLineCsvGeneratorJobType.Released, false)]
        [DataRow(FundingLineCsvGeneratorJobType.History, false)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentOrganisationGroupValues, false)]
        [DataRow(FundingLineCsvGeneratorJobType.HistoryOrganisationGroupValues, true)]
        public void SupportsCurrentStateJobType(FundingLineCsvGeneratorJobType jobType,
            bool expectedSupportsFlag)
        {
            _transformation.IsForJobType(jobType)
                .Should()
                .Be(expectedSupportsFlag);
        }

        [TestMethod]
        [DataRow(CalculateFunding.Models.Publishing.GroupingReason.Contracting)]
        [DataRow(CalculateFunding.Models.Publishing.GroupingReason.Payment)]
        [DataRow(CalculateFunding.Models.Publishing.GroupingReason.Information)]
        public void FlattensTemplateCalculationsAndProviderMetaDataIntoRows(
            CalculateFunding.Models.Publishing.GroupingReason expectedGroupingReason)
        {
            PublishedFundingVersion publishedFunding =
                NewPublishedFundingVersion(pfv =>
                    pfv.WithOrganisationGroupTypeCode(OrganisationGroupTypeCode.LocalAuthority)
                        .WithOrganisationGroupName("Enfield")
                        .WithGroupReason(expectedGroupingReason)
                        .WithProviderFundings(new [] { "one", "two" })
                        .WithPublishedProviderStatus(PublishedFundingStatus.Released)
                        .WithMajor(1)
                        .WithAuthor(NewReference(rf => rf.WithName("system")))
                        .WithDate("2020-02-05T20:03:55")
                        .WithFundingLines(
                            NewFundingLines(fl =>
                                    fl.WithName("fundingLine1")
                                        .WithValue(123M),
                                fl =>
                                    fl.WithName("fundingLine2")
                                        .WithValue(456M))));

            dynamic[] expectedCsvRows =
            {
                new Dictionary<string, object>
                {
                    {"Grouping Reason", expectedGroupingReason.ToString()},
                    {"Grouping Code", "LocalAuthority"},
                    {"Grouping Name", "Enfield"},
                    {"Allocation Status", "Released"},
                    {"Allocation Major Version", "1"},
                    {"Allocation Author", "system"},
                    {"Allocation DateTime", "2020-02-05T20:03:55"},
                    {"Provider Count", 2},
                    {"fundingLine1", 123M.ToString(CultureInfo.InvariantCulture)}, //funding lines to be alpha numerically ordered on name
                    {"fundingLine2", 456M.ToString(CultureInfo.InvariantCulture)}
                }
            };

            ExpandoObject[] transformProviderResultsIntoCsvRows = _transformation.Transform(new [] { publishedFunding }).ToArray();

            transformProviderResultsIntoCsvRows
                .Should()
                .BeEquivalentTo(expectedCsvRows);
        }
    }
}