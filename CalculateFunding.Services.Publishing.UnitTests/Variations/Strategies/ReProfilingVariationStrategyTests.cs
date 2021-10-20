using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Variations.Changes;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Strategies
{
    [TestClass]
    public class MidYearReProfilingVariationStrategyTests : ReProfilingVariationStrategyTestsBase
    {
        [TestInitialize]
        public void SetUp()
        {
            VariationStrategy = new MidYearReProfilingVariationStrategy();
        }    
        
        [TestMethod]
        public async Task FailsPreconditionCheckIfIsNotANewOpenerAndHasNoNewPaymentFundingLines()
        {
            GivenTheOtherwiseValidVariationContext();
            
            AndTheVariationPointers(NewVariationPointer(_ => _.WithYear(NewRandomNumber())
                .WithTypeValue(NewRandomMonth())
                .WithOccurence(1)
                .WithFundingLineId(FundingLineCode)
                .WithPeriodType(ProfilePeriodType.CalendarMonth.ToString())));
            AndTheRefreshStateFundingLines(NewFundingLine(_ => _.WithFundingLineCode(FundingLineCode)
                .WithFundingLineType(FundingLineType.Payment)
                .WithValue(NewRandomNumber())
                .WithDistributionPeriods(NewDistributionPeriod(dp =>
                    dp.WithProfilePeriods(NewProfilePeriod())))));

            await WhenTheVariationsAreProcessed();

            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
            VariationContext
                .VariationReasons
                .Should()
                .BeEmpty();
        }
        
        [TestMethod]
        public async Task TracksAllAsAffectedFundingLineCodesAndQueuesMidYearReProfileVariationChangeIfIsNewOpener()
        {
            int year = NewRandomNumber();
            string month = NewRandomMonth();

            GivenTheOtherwiseValidVariationContext(_ =>
            {
                _.AllPublishedProviderSnapShots.Clear();
                _.UpdatedProvider.Status = Publishing.Variations.Strategies.VariationStrategy.Opened;
            });
            AndTheRefreshStateFundingLines(NewFundingLine(),
                NewFundingLine(_ => _.WithFundingLineCode(FundingLineCode)
                    .WithFundingLineType(FundingLineType.Payment)
                    .WithValue(NewRandomNumber())
                    .WithDistributionPeriods(NewDistributionPeriod(dp =>
                        dp.WithProfilePeriods(NewProfilePeriod(pp => pp.WithYear(year)
                                .WithTypeValue(month)
                                .WithType(ProfilePeriodType.CalendarMonth)
                                .WithOccurence(0)),
                            NewProfilePeriod(pp => pp.WithYear(year)
                                .WithTypeValue(month)
                                .WithType(ProfilePeriodType.CalendarMonth)
                                .WithOccurence(1)))))));
            AndTheVariationPointers(NewVariationPointer(_ => _.WithYear(year)
                .WithTypeValue(month)
                .WithOccurence(1)
                .WithFundingLineId(FundingLineCode)
                .WithPeriodType(ProfilePeriodType.CalendarMonth.ToString())));

            await WhenTheVariationsAreProcessed();

            ThenTheVariationChangeWasQueued<MidYearReProfileVariationChange>();
            AndTheAffectedFundingLinesWereTracked(FundingLineCode);
        }
        
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task TracksNewFundingLinesAsAffectedFundingLineCodesAndQueuesMidYearReProfileVariationChangeIfHasNewFundingLines(bool generateNewFundingLine)
        {
            int year = NewRandomNumber();
            string month = NewRandomMonth();
            string newFundingLineCode = NewRandomString();

            base.GivenTheOtherwiseValidVariationContext(_ => _.UpdatedProvider.Status = Publishing.Variations.Strategies.VariationStrategy.Opened);

            // use an existing funding line code but which wasn't previously funded
            if (!generateNewFundingLine)
            {
                VariationContext.GetPublishedProviderOriginalSnapShot(VariationContext.ProviderId).Released.FundingLines.ForEach(_ => _.Value = null);
                newFundingLineCode = FundingLineCode;
            }

            AndTheRefreshStateFundingLines(NewFundingLine(_ => _.WithFundingLineCode(newFundingLineCode)
                    .WithFundingLineType(FundingLineType.Payment)
                    .WithValue(NewRandomNumber())
                    .WithDistributionPeriods(NewDistributionPeriod(dp =>
                        dp.WithProfilePeriods(NewProfilePeriod(pp => pp.WithYear(year)
                                .WithTypeValue(month)
                                .WithType(ProfilePeriodType.CalendarMonth)
                                .WithOccurence(0)),
                            NewProfilePeriod(pp => pp.WithYear(year)
                                .WithTypeValue(month)
                                .WithType(ProfilePeriodType.CalendarMonth)
                                .WithOccurence(1)))))));
            AndTheVariationPointers(NewVariationPointer(_ => _.WithYear(year)
                .WithTypeValue(month)
                .WithOccurence(1)
                .WithFundingLineId(newFundingLineCode)
                .WithPeriodType(ProfilePeriodType.CalendarMonth.ToString())));

            await WhenTheVariationsAreProcessed();

            ThenTheVariationChangeWasQueued<MidYearReProfileVariationChange>();
            AndTheAffectedFundingLinesWereTracked(newFundingLineCode);
        }
    }

    [TestClass]
    public class MidYearClosureReProfilingVariationStrategyTests : ReProfilingVariationStrategyTestsBase
    {
        [TestInitialize]
        public void SetUp()
        {
            VariationStrategy = new MidYearClosureReProfilingVariationStrategy();
        }

        [TestMethod]
        public async Task FailsPreconditionCheckIfItHasNoAllocations()
        {
            GivenTheOtherwiseValidVariationContext(_ => _.UpdatedProvider.Status = Closed);

            AndTheVariationPointers(NewVariationPointer(_ => _.WithYear(NewRandomNumber())
                .WithTypeValue(NewRandomMonth())
                .WithOccurence(1)
                .WithFundingLineId(FundingLineCode)
                .WithPeriodType(ProfilePeriodType.CalendarMonth.ToString())));
            AndTheReleaseStateFundingLines(NewFundingLine(_ => _.WithFundingLineCode(FundingLineCode)
                .WithFundingLineType(FundingLineType.Payment)
                .WithValue(null)
                .WithDistributionPeriods(NewDistributionPeriod(dp =>
                    dp.WithProfilePeriods(NewProfilePeriod())))));

            await WhenTheVariationsAreProcessed();

            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
            VariationContext
                .VariationReasons
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task TracksFundingLinesAsAffectedFundingLineCodesAndQueuesMidYearClosureReProfileVariationChangeIfProviderClosed()
        {
            int year = NewRandomNumber();
            string month = NewRandomMonth();
            string newFundingLineCode = NewRandomString();

            base.GivenTheOtherwiseValidVariationContext(_ => _.UpdatedProvider.Status = Publishing.Variations.Strategies.VariationStrategy.Closed);

            AndTheReleaseStateFundingLines(NewFundingLine(_ => _.WithFundingLineCode(newFundingLineCode)
                    .WithFundingLineType(FundingLineType.Payment)
                    .WithValue(NewRandomNumber())
                    .WithDistributionPeriods(NewDistributionPeriod(dp =>
                        dp.WithProfilePeriods(NewProfilePeriod(pp => pp.WithYear(year)
                                .WithTypeValue(month)
                                .WithType(ProfilePeriodType.CalendarMonth)
                                .WithOccurence(0)),
                            NewProfilePeriod(pp => pp.WithYear(year)
                                .WithTypeValue(month)
                                .WithType(ProfilePeriodType.CalendarMonth)
                                .WithOccurence(1)))))));
            AndTheVariationPointers(NewVariationPointer(_ => _.WithYear(year)
                .WithTypeValue(month)
                .WithOccurence(1)
                .WithFundingLineId(newFundingLineCode)
                .WithPeriodType(ProfilePeriodType.CalendarMonth.ToString())));

            await WhenTheVariationsAreProcessed();

            ThenTheVariationChangeWasQueued<MidYearReProfileVariationChange>();
            AndTheAffectedFundingLinesWereTracked(newFundingLineCode);
        }
    }

    [TestClass]
    public class ReProfilingVariationStrategyTests : ReProfilingVariationStrategyTestsBase
    {
        [TestInitialize]
        public void SetUp()
        {
            VariationStrategy = new ReProfilingVariationStrategy();
        }

        [TestMethod]
        public async Task FailsPreconditionCheckIfThereAreNoPaidPeriods()
        {
           int year = NewRandomNumber();
            string month = NewRandomMonth();

            GivenTheOtherwiseValidVariationContext(_ => _.UpdatedProvider.Status = NewRandomString());
            AndTheRefreshStateFundingLines(NewFundingLine(),
                NewFundingLine(_ => _.WithFundingLineCode(FundingLineCode)
                    .WithFundingLineType(FundingLineType.Payment)
                    .WithValue(NewRandomNumber())
                    .WithDistributionPeriods(NewDistributionPeriod(dp =>
                        dp.WithProfilePeriods(NewProfilePeriod(pp => pp.WithYear(year)
                                .WithTypeValue(month)
                                .WithType(ProfilePeriodType.CalendarMonth)
                                .WithOccurence(0)),
                            NewProfilePeriod(pp => pp.WithYear(year)
                                .WithTypeValue(month)
                                .WithType(ProfilePeriodType.CalendarMonth)
                                .WithOccurence(1)))))));
            AndThePriorStateFundingLines(NewFundingLine(),
                NewFundingLine(_ => _.WithFundingLineCode(FundingLineCode)
                    .WithFundingLineType(FundingLineType.Payment)
                    .WithValue(NewRandomNumber())
                    .WithDistributionPeriods(NewDistributionPeriod(dp =>
                        dp.WithProfilePeriods(NewProfilePeriod(pp => pp.WithYear(year)
                                .WithTypeValue(month)
                                .WithType(ProfilePeriodType.CalendarMonth)
                                .WithOccurence(0)),
                            NewProfilePeriod(pp => pp.WithYear(year)
                                .WithTypeValue(month)
                                .WithType(ProfilePeriodType.CalendarMonth)
                                .WithOccurence(1)))))));
            AndTheVariationPointers(NewVariationPointer(_ => _.WithYear(year)
                .WithTypeValue(month)
                .WithOccurence(0)
                .WithFundingLineId(FundingLineCode)
                .WithPeriodType(ProfilePeriodType.CalendarMonth.ToString())));

            await WhenTheVariationsAreProcessed();


            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
            VariationContext
                .VariationReasons
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task TracksAffectedFundingLineCodesAndQueuesReProfileVariationChangeIfProfilingDiffers()
        {
            int year = NewRandomNumber();
            string month = NewRandomMonth();

            GivenTheOtherwiseValidVariationContext(_ => _.UpdatedProvider.Status = NewRandomString());
            AndTheRefreshStateFundingLines(NewFundingLine(),
                NewFundingLine(_ => _.WithFundingLineCode(FundingLineCode)
                    .WithFundingLineType(FundingLineType.Payment)
                    .WithValue(NewRandomNumber())
                    .WithDistributionPeriods(NewDistributionPeriod(dp =>
                        dp.WithProfilePeriods(NewProfilePeriod(pp => pp.WithYear(year)
                                .WithTypeValue(month)
                                .WithType(ProfilePeriodType.CalendarMonth)
                                .WithOccurence(0)),
                            NewProfilePeriod(pp => pp.WithYear(year)
                                .WithTypeValue(month)
                                .WithType(ProfilePeriodType.CalendarMonth)
                                .WithOccurence(1)))))));
            AndThePriorStateFundingLines(NewFundingLine(),
                NewFundingLine(_ => _.WithFundingLineCode(FundingLineCode)
                    .WithFundingLineType(FundingLineType.Payment)
                    .WithValue(NewRandomNumber())
                    .WithDistributionPeriods(NewDistributionPeriod(dp =>
                        dp.WithProfilePeriods(NewProfilePeriod(pp => pp.WithYear(year)
                                .WithTypeValue(month)
                                .WithType(ProfilePeriodType.CalendarMonth)
                                .WithOccurence(0)),
                            NewProfilePeriod(pp => pp.WithYear(year)
                                .WithTypeValue(month)
                                .WithType(ProfilePeriodType.CalendarMonth)
                                .WithOccurence(1)))))));
            AndTheVariationPointers(NewVariationPointer(_ => _.WithYear(year)
                .WithTypeValue(month)
                .WithOccurence(1)
                .WithFundingLineId(FundingLineCode)
                .WithPeriodType(ProfilePeriodType.CalendarMonth.ToString())));

            await WhenTheVariationsAreProcessed();

            ThenTheVariationChangeWasQueued<ReProfileVariationChange>();
            AndTheAffectedFundingLinesWereTracked(FundingLineCode);
        }
    }
}