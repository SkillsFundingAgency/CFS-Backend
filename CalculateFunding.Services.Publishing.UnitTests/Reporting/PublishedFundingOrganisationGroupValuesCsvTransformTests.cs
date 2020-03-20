using AutoMapper;
using CalculateFunding.Common.Models;
using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using CalculateFunding.Services.Publishing.UnitTests.Reporting.FundingLines;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting
{
    [TestClass]
    public class PublishedFundingOrganisationGroupValuesCsvTransformTests : FundingLineCsvTransformTestBase
    {
        private PublishedFundingOrganisationGroupValuesCsvTransform _transformation;
        private IMapper _mapper;

        [TestInitialize]
        public void SetUp()
        {
            _transformation = new PublishedFundingOrganisationGroupValuesCsvTransform();

            _mapper = new MapperConfiguration(_ =>
            {
                _.AddProfile<PublishingServiceMappingProfile>();
            }).CreateMapper();
        }

        [TestMethod]
        [DataRow(FundingLineCsvGeneratorJobType.Undefined, false)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentState, false)]
        [DataRow(FundingLineCsvGeneratorJobType.Released, false)]
        [DataRow(FundingLineCsvGeneratorJobType.History, false)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentOrganisationGroupValues, true)]
        [DataRow(FundingLineCsvGeneratorJobType.HistoryOrganisationGroupValues, true)]
        public void SupportsCurrentStateJobType(FundingLineCsvGeneratorJobType jobType,
            bool expectedSupportsFlag)
        {
            _transformation.IsForJobType(jobType)
                .Should()
                .Be(expectedSupportsFlag);
        }

        [TestMethod]
        public void FlattensTemplateCalculationsAndProviderMetaDataIntoRows()
        {
            IEnumerable<PublishedFundingOrganisationGrouping> publishedFundingOrganisationGroupings =
                NewPublishedFundingOrganisationGroupings(_ =>
                _.WithPublishedFundingVersions(
                    NewPublishedFundingVersions(pfv =>
                           pfv.WithOrganisationGroupTypeCode(OrganisationGroupTypeCode.LocalAuthority)
                           .WithOrganisationGroupName("Enfield")
                           .WithPublishedProviderStatus(PublishedFundingStatus.Released)
                           .WithMajor(1)
                           .WithAuthor(new Reference { Name = "system" })
                           .WithDate("2020-02-05T20:03:55")
                           .WithFundingLines(
                               NewFundingLines(fl =>
                                   fl.WithName("fundingLine1")
                                   .WithValue(123M),
                               fl =>
                                   fl.WithName("fundingLine2")
                                   .WithValue(456M)))))
                .WithOrganisationGroupResult(
                    NewOrganisationGroupResult(ogp =>
                    ogp.WithProviders(
                        _mapper.Map<IEnumerable<Common.ApiClient.Providers.Models.Provider>>(NewProviders(p =>
                            p.WithName("p1"),
                        p =>
                            p.WithName("p2")))))));

            dynamic[] expectedCsvRows =
{
                new Dictionary<string, object>
                {
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

            ExpandoObject[] transformProviderResultsIntoCsvRows = _transformation.Transform(publishedFundingOrganisationGroupings).ToArray();

            transformProviderResultsIntoCsvRows
            .Should()
            .BeEquivalentTo(expectedCsvRows,
                cfg => cfg.WithStrictOrdering());
        }

    }
}
