using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.SqlExport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.SqlExport
{
    [TestClass]
    public class ProfilingDataTableBuilderTests : DataTableBuilderTest<ProfilingDataTableBuilder>
    {
        private uint _templateLineId;
        private string _fundingLineCode;

        [TestInitialize]
        public void SetUp()
        {
            _templateLineId = NewRandomUnsignedNumber();
            _fundingLineCode = NewRandomString();

            DataTableBuilder = new ProfilingDataTableBuilder(_templateLineId, _fundingLineCode);
        }

        [TestMethod]
        public void MapsPaymentFundingLinesIntoDataTable()
        {
            string periodOne = "January";
            string periodTwo = "February";

            int year = NewRandomYear();

            int occurrenceOne = NewRandomNumber();
            int occurrenceTwo = NewRandomNumber();

            string distributionPeriodId = NewRandomString();

            decimal valueOne = NewRandomNumber();
            decimal valueTwo = NewRandomNumber();
            decimal valueThree = NewRandomNumber();
            decimal valueFour = NewRandomNumber();

            FundingLine paymentFundingLineOne = NewFundingLine(_ => _.WithOrganisationGroupingReason(OrganisationGroupingReason.Payment)
                .WithTemplateLineId(_templateLineId)
                .WithFundingLineCode(_fundingLineCode)
                .WithDistributionPeriods(NewDistributionPeriod(dp => 
                    dp.WithDistributionPeriodId(distributionPeriodId)
                        .WithProfilePeriods(NewProfilePeriod(pp => pp.WithType(ProfilePeriodType.CalendarMonth)
                            .WithTypeValue(periodOne)
                            .WithOccurence(occurrenceOne)
                            .WithAmount(valueOne)
                            .WithYear(year)), 
                        NewProfilePeriod(pp => pp.WithType(ProfilePeriodType.CalendarMonth)
                            .WithTypeValue(periodTwo)
                            .WithOccurence(occurrenceTwo)
                            .WithAmount(valueTwo)
                            .WithYear(year))))));
            FundingLine paymentFundingLineTwo = NewFundingLine(_ => _.WithOrganisationGroupingReason(OrganisationGroupingReason.Payment));
            FundingLine paymentFundingLineThree = NewFundingLine(_ => _.WithOrganisationGroupingReason(OrganisationGroupingReason.Payment)
                .WithTemplateLineId(_templateLineId)
                .WithFundingLineCode(_fundingLineCode)
                .WithDistributionPeriods(NewDistributionPeriod(dp => 
                    dp.WithDistributionPeriodId(distributionPeriodId)
                        .WithProfilePeriods(NewProfilePeriod(pp => pp.WithType(ProfilePeriodType.CalendarMonth)
                            .WithTypeValue(periodOne)
                            .WithOccurence(occurrenceOne)
                            .WithAmount(valueThree)
                            .WithYear(year)), 
                        NewProfilePeriod(pp => pp.WithType(ProfilePeriodType.CalendarMonth)
                            .WithTypeValue(periodTwo)
                            .WithOccurence(occurrenceTwo)
                            .WithAmount(valueFour)
                            .WithYear(year))))));
            FundingLine paymentFundingLineFour = NewFundingLine(_ => _.WithOrganisationGroupingReason(OrganisationGroupingReason.Payment));

            PublishedProviderVersion rowOne = NewPublishedProviderVersion(_ => _.WithFundingLines(paymentFundingLineOne,
                    NewFundingLine(fundingLineBuilder => fundingLineBuilder.WithOrganisationGroupingReason(OrganisationGroupingReason.Information)),
                    paymentFundingLineTwo)
                .WithFundingStreamId(FundingStreamId)
                .WithFundingPeriodId(FundingPeriodId));
            PublishedProviderVersion rowTwo = NewPublishedProviderVersion(_ => _.WithFundingLines(paymentFundingLineThree,
                    NewFundingLine(fundingLineBuilder => fundingLineBuilder.WithOrganisationGroupingReason(OrganisationGroupingReason.Information)),
                    paymentFundingLineFour)
                .WithFundingStreamId(FundingStreamId)
                .WithFundingPeriodId(FundingPeriodId));

            WhenTheRowsAreAdded(rowOne, rowTwo);

            string profilePeriodOnePrefix = $"{periodOne}_{ProfilePeriodType.CalendarMonth}_{year}_{occurrenceOne}";
            string profilePeriodTwoPrefix = $"{periodTwo}_{ProfilePeriodType.CalendarMonth}_{year}_{occurrenceTwo}";

            ThenTheDataTableHasColumnsMatching(NewDataColumn<string>("PublishedProviderId", 128),
                NewDataColumn<string>($"{profilePeriodOnePrefix}_Period", 64, true),
                NewDataColumn<string>($"{profilePeriodOnePrefix}_PeriodType", 64, true),
                NewDataColumn<string>($"{profilePeriodOnePrefix}_Year", 64, true),
                NewDataColumn<string>($"{profilePeriodOnePrefix}_Occurence", 64, true),
                NewDataColumn<string>($"{profilePeriodOnePrefix}_DistributionPeriod", 64, true),
                NewDataColumn<string>($"{profilePeriodOnePrefix}_Value",  allowNull: true),
                NewDataColumn<string>($"{profilePeriodTwoPrefix}_Period", 64, true),
                NewDataColumn<string>($"{profilePeriodTwoPrefix}_PeriodType", 64, true),
                NewDataColumn<string>($"{profilePeriodTwoPrefix}_Year", 64, true),
                NewDataColumn<string>($"{profilePeriodTwoPrefix}_Occurence", 64, true),
                NewDataColumn<string>($"{profilePeriodTwoPrefix}_DistributionPeriod", 64, true),
                NewDataColumn<string>($"{profilePeriodTwoPrefix}_Value",  allowNull: true));
            AndTheDataTableHasRowsMatching(NewRow(rowOne.PublishedProviderId, periodOne, ProfilePeriodType.CalendarMonth.ToString(), year, occurrenceOne, distributionPeriodId, valueOne,
                periodTwo, ProfilePeriodType.CalendarMonth.ToString(), year, occurrenceTwo, distributionPeriodId, valueTwo ),
                NewRow(rowOne.PublishedProviderId, periodOne, ProfilePeriodType.CalendarMonth.ToString(), year, occurrenceOne, distributionPeriodId, valueThree,
                    periodTwo, ProfilePeriodType.CalendarMonth.ToString(), year, occurrenceTwo, distributionPeriodId, valueFour ));
            AndTheTableNameIs($"[dbo].[{FundingStreamId}_{FundingPeriodId}_Profiles_{_fundingLineCode}]");
        }
    }
}