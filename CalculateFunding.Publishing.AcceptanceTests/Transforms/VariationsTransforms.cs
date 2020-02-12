using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Publishing.AcceptanceTests.Models;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace CalculateFunding.Publishing.AcceptanceTests.Transforms
{
    [Binding]
    public class VariationsTransforms : TransformsBase
    {
        [StepArgumentTransformation]
        public IEnumerable<ExpectedFundingLineOverPayment> ToExpectedFundingLineOverPayments(
            Table fundingLineOverPaymentsTable)
        {
            EnsureTableHasData(fundingLineOverPaymentsTable);

            return fundingLineOverPaymentsTable.CreateSet<ExpectedFundingLineOverPayment>()
                .ToArray();
        }

        [StepArgumentTransformation]
        public IEnumerable<ExpectedFundingLineProfileValues> ToExpectedFundingLineProfileValues(
            Table fundingLineProfileValuesTable)
        {
            EnsureTableHasData(fundingLineProfileValuesTable);

            return fundingLineProfileValuesTable.Rows.GroupBy(_ => _["FundingLineCode"])
                .Select(_ => new ExpectedFundingLineProfileValues
                {
                    FundingLineCode = _.Key,
                    ExpectedDistributionPeriods = _.GroupBy(fl => fl["DistributionPeriodId"])
                        .Select(dp => new ExpectedDistributionPeriod
                        {
                            DistributionPeriodId = dp.Key,
                            ExpectedProfileValues = dp.Select(pv => new ExpectedProfileValue
                            {
                                Type = pv["Type"].AsEnum<ProfilePeriodType>(),
                                TypeValue = pv["TypeValue"],
                                Year = pv["Year"].AsInt(),
                                Occurrence = pv["Occurrence"].AsInt(),
                                ProfiledValue = pv["ProfiledValue"].AsDecimal()
                            })
                        })
                })
                .ToArray();
        }

        [StepArgumentTransformation]
        public IEnumerable<ExpectedFundingLineTotal> ToExpectedFundingLineTotals(Table fundingLineTotalsTable)
        {
            EnsureTableHasData(fundingLineTotalsTable);

            return fundingLineTotalsTable.CreateSet<ExpectedFundingLineTotal>()
                .ToArray();
        }

        [StepArgumentTransformation]
        public IEnumerable<ProfileVariationPointer> ToVariationPointers(Table variationPointersTable)
        {
            EnsureTableHasData(variationPointersTable);

            return variationPointersTable.CreateSet<ProfileVariationPointer>();
        }

        [StepArgumentTransformation]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public IEnumerable<ExpectedVariationReasons> ToExpectedVariationReasons(Table variationReasonsTable)
        {
           EnsureTableHasData(variationReasonsTable);

           return variationReasonsTable.Rows.GroupBy(_ => _["ProviderId"])
               .Select(_ => new ExpectedVariationReasons
               {
                   ProviderId = _.Key,
                   Reasons = _.Select(reason => reason["VariationReason"].AsEnum<VariationReason>())
               }).ToArray();
        }
    }
}