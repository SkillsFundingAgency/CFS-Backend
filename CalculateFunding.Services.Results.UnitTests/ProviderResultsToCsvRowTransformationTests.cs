using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.ProviderLegacy;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Results.UnitTests
{
    [TestClass]
    public class ProviderResultsToCsvRowTransformationTests
    {
        private ProviderResultsToCsvRowsTransformation _transformation;

        [TestInitialize]
        public void SetUp()
        {
            _transformation = new ProviderResultsToCsvRowsTransformation();
        }

        [TestMethod]
        public void FlattensTemplateCalculationsAndProviderMetaDataIntoRows()
        {
            IEnumerable<ProviderResult> providerResults = NewProviderResults(_ => _.WithProviderSummary(
                        NewProviderSummary(
                            ps => ps.WithName("one")
                                .WithEstablishmentNumber("en1")
                                .WithLACode("lacode1")
                                .WithAuthority("laname1")
                                .WithURN("urn1")
                                .WithUKPRN("ukprn1")
                                .WithLocalProviderType("pt1")
                                .WithLocalProviderSubType("pst1")))
                    .WithCalculationResults(NewCalculationResult(cr => cr.WithCalculationType(CalculationType.Additional)),
                        NewCalculationResult(cr => cr.WithCalculationType(CalculationType.Template)
                            .WithCalculation(NewReference(rf => rf.WithName("calc1")))
                            .WithValue(999M)),
                        NewCalculationResult(cr => cr.WithCalculationType(CalculationType.Template)
                            .WithCalculation(NewReference(rf => rf.WithName("calc2")))
                            .WithValue(666M))),
                _ => _.WithProviderSummary(
                        NewProviderSummary(
                            ps => ps.WithName("two")
                                .WithEstablishmentNumber("en2")
                                .WithLACode("lacode2")
                                .WithAuthority("laname2")
                                .WithURN("urn2")
                                .WithUKPRN("ukprn2")
                                .WithLocalProviderType("pt2")
                                .WithLocalProviderSubType("pst2")))
                    .WithCalculationResults(NewCalculationResult(cr => cr.WithCalculationType(CalculationType.Additional)),
                        NewCalculationResult(cr => cr.WithCalculationType(CalculationType.Template)
                            .WithCalculation(NewReference(rf => rf.WithName("calc1")))
                            .WithValue(123M)),
                        NewCalculationResult(cr => cr.WithCalculationType(CalculationType.Template)
                            .WithCalculation(NewReference(rf => rf.WithName("calc2")))
                            .WithValue(null)))
                    .WithFundingLineResults(
                        NewFundingLineResult(flr => flr
                            .WithFundingLine(NewReference(rf => rf.WithName("fundingLine1")))
                            .WithValue(333M)),
                        NewFundingLineResult(flr => flr
                            .WithFundingLine(NewReference(rf => rf.WithName("fundingLine2")))
                            .WithValue(555M))));

            dynamic[] expectedCsvRows = {
                new Dictionary<string, object>
                {
                    {"UKPRN", "ukprn1"},
                    {"URN", "urn1"},
                    {"Estab Number", "en1"},
                    {"Provider Name", "one"},
                    {"LA Code", "lacode1"},
                    {"LA Name", "laname1"},
                    {"Provider Type", "pt1"},
                    {"Provider SubType", "pst1"},
                    {"calc1", 999M.ToString(CultureInfo.InvariantCulture)},
                    {"calc2", 666M.ToString(CultureInfo.InvariantCulture)},
                },
                new Dictionary<string, object>
                {
                    {"UKPRN", "ukprn2"},
                    {"URN", "urn2"},
                    {"Estab Number", "en2"},
                    {"Provider Name", "two"},
                    {"LA Code", "lacode2"},
                    {"LA Name", "laname2"},
                    {"Provider Type", "pt2"},
                    {"Provider SubType", "pst2"},
                    {"FL fundingLine1", 333M.ToString(CultureInfo.InvariantCulture)},
                    {"FL fundingLine2", 555M.ToString(CultureInfo.InvariantCulture)},
                    {"calc1", 123M.ToString(CultureInfo.InvariantCulture)},
                    {"calc2", null},
                }
            };

            ExpandoObject[] transformProviderResultsIntoCsvRows = _transformation.TransformProviderResultsIntoCsvRows(providerResults).ToArray();

            transformProviderResultsIntoCsvRows
                .Should()
                .BeEquivalentTo(expectedCsvRows,
                    cfg => cfg.WithStrictOrdering());
        }

        private static IEnumerable<ProviderResult> NewProviderResults(params Action<ProviderResultBuilder>[] setUps)
        {
            return setUps.Select(NewProviderResult);
        }

        private static ProviderResult NewProviderResult(Action<ProviderResultBuilder> setUp = null)
        {
            ProviderResultBuilder providerResultBuilder = new ProviderResultBuilder();

            setUp?.Invoke(providerResultBuilder);

            return providerResultBuilder.Build();
        }

        private static ProviderSummary NewProviderSummary(Action<ProviderSummaryBuilder> setUp = null)
        {
            ProviderSummaryBuilder providerSummaryBuilder = new ProviderSummaryBuilder();

            setUp?.Invoke(providerSummaryBuilder);

            return providerSummaryBuilder.Build();
        }

        private static CalculationResult NewCalculationResult(Action<CalculationResultBuilder> setUp = null)
        {
            CalculationResultBuilder calculationResultBuilder = new CalculationResultBuilder();

            setUp?.Invoke(calculationResultBuilder);

            return calculationResultBuilder.Build();
        }

        private static FundingLineResult NewFundingLineResult(Action<FundingLineResultBuilder> setUp = null)
        {
            FundingLineResultBuilder fundingLineResultBuilder = new FundingLineResultBuilder();

            setUp?.Invoke(fundingLineResultBuilder);

            return fundingLineResultBuilder.Build();
        }

        private static Reference NewReference(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();

            setUp?.Invoke(referenceBuilder);

            return referenceBuilder.Build();
        }
    }
}