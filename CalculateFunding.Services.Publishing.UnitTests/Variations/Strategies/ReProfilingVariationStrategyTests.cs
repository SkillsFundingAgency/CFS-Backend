using System;
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
    public class ConverterReProfilingVariationStrategyTests : ReProfilingVariationStrategyTestsBase
    {
        protected override string Strategy => "ConverterReProfiling";

        [TestInitialize]
        public void SetUp()
        {
            VariationStrategy = new ConverterReProfilingVariationStrategy();
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
        public async Task TracksAllAsAffectedFundingLineCodesAndQueuesConverterReProfileVariationChangeIfIsNewConverterOpener()
        {
            int year = NewRandomNumber();
            string month = NewRandomMonth();
            DateTimeOffset openedDate = DateTime.Now;

            GivenTheOtherwiseValidVariationContext(_ =>
            {
                _.AllPublishedProviderSnapShots.Clear();
                _.UpdatedProvider.Status = Publishing.Variations.Strategies.VariationStrategy.Opened;
                _.RefreshState.Provider = NewProvider(_ => _.WithDateOpened(openedDate));
                _.RefreshState.Provider.ReasonEstablishmentOpened = Publishing.Variations.Strategies.VariationStrategy.AcademyConverter;
                _.FundingPeriodStartDate = openedDate.AddMonths(-1);
                _.FundingPeriodEndDate = openedDate.AddMonths(1);
                _.RefreshState.Provider.Predecessors = new[] { NewProvider().ProviderId };
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

            ThenTheVariationChangeWasQueued<ConverterReProfileVariationChange>();
            AndTheAffectedFundingLinesWereTracked(FundingLineCode);
        }

        [TestMethod]
        public async Task DoesntTrackFundingLinesAsAffectedFundingLineCodeWhenRefreshFundingLineValueIsZero()
        {
            int year = NewRandomNumber();
            string month = NewRandomMonth();
            string newFundingLineCode = NewRandomString();
            string newFundingLineCodeNotFunded = NewRandomString();
            DateTimeOffset openedDate = DateTime.Now;

            base.GivenTheOtherwiseValidVariationContext(_ =>
            {
                _.UpdatedProvider.Status = Publishing.Variations.Strategies.VariationStrategy.Opened;
                _.RefreshState.Provider = NewProvider(_ => _.WithDateOpened(openedDate));
                _.RefreshState.Provider.ReasonEstablishmentOpened = Publishing.Variations.Strategies.VariationStrategy.AcademyConverter;
                _.FundingPeriodStartDate = openedDate.AddMonths(-1);
                _.FundingPeriodEndDate = openedDate.AddMonths(1);
                _.RefreshState.Provider.Predecessors = new[] { NewProvider().ProviderId };
            });

            VariationContext.GetPublishedProviderOriginalSnapShot(VariationContext.ProviderId).Released = null;

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
                                    .WithOccurence(1)))))),
                    NewFundingLine(_ => _.WithFundingLineCode(newFundingLineCodeNotFunded)
                        .WithFundingLineType(FundingLineType.Payment)
                        .WithValue(0)
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

            ThenTheVariationChangeWasQueued<ConverterReProfileVariationChange>();

            AndTheAffectedFundingLinesAreNotTracked(newFundingLineCodeNotFunded);
        }
    }

    [TestClass]
    public class MidYearReProfilingVariationStrategyTests : ReProfilingVariationStrategyTestsBase
    {
        protected override string Strategy => "MidYearReProfiling";

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
        public async Task DoesntTrackFundingLinesAsAffectedFundingLineCodeWhenRefreshFundingLineValueIsZero(bool hasPreviousReleasedProvider)
        {
            int year = NewRandomNumber();
            string month = NewRandomMonth();
            string newFundingLineCode = NewRandomString();
            string newFundingLineCodeNotFunded = NewRandomString();

            base.GivenTheOtherwiseValidVariationContext(_ => _.UpdatedProvider.Status = Publishing.Variations.Strategies.VariationStrategy.Opened);

            if (!hasPreviousReleasedProvider)
            {
                VariationContext.GetPublishedProviderOriginalSnapShot(VariationContext.ProviderId).Released = null;
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
                                    .WithOccurence(1)))))),
                    NewFundingLine(_ => _.WithFundingLineCode(newFundingLineCodeNotFunded)
                        .WithFundingLineType(FundingLineType.Payment)
                        .WithValue(0)
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

            AndTheAffectedFundingLinesAreNotTracked(newFundingLineCodeNotFunded);
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
        protected override string Strategy => "MidYearClosureReProfiling";

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

            ThenTheVariationChangeWasQueued<MidYearClosureReProfileVariationChange>();
            AndTheAffectedFundingLinesWereTracked(newFundingLineCode);
        }
    }

    [TestClass]
    public class ReProfilingVariationStrategyTests : ReProfilingVariationStrategyTestsBase
    {
        protected override string Strategy => "ReProfiling";

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