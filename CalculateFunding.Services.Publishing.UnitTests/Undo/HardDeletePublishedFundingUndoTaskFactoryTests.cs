using CalculateFunding.Services.Publishing.Interfaces.Undo;
using CalculateFunding.Services.Publishing.Undo;
using CalculateFunding.Services.Publishing.Undo.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Publishing.UnitTests.Undo
{
    [TestClass]
    public class HardDeletePublishedFundingUndoTaskFactoryTests : PublishedFundingUndoTaskFactoryTest
    {
        [TestInitialize]
        public void SetUp()
        {
            Factory = new HardDeletePublishedFundingUndoTaskFactory(Cosmos.Object,
                BlobStore.Object,
                ProducerConsumerFactory,
                Logger,
                JobTracker.Object);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void IsForHardDeleteJobs(bool hardDelete)
        {
            GivenTheParametersIsHardDelete(hardDelete);

            bool isForJob = WhenIsForJobIsQueried();

            isForJob
                .Should()
                .Be(hardDelete);
        }

        [TestMethod]
        public void CreatesHardDeletePublishedProviderUndoTasks()
        {
            TheTaskIs<PublishedProviderUndoTask>(Factory.CreatePublishedProviderUndoTask(),
                (_ => _.IsHardDelete, nameof(PublishedProviderUndoTask.IsHardDelete)));
        }

        [TestMethod]
        public void CreatesHardDeletePublishedProviderVersionUndoTasks()
        {
            TheTaskIs<HardDeletePublishedProviderVersionsUndoTask>(Factory.CreatePublishedProviderVersionUndoTask());
        }

        [TestMethod]
        public void CreatesHardDeletePublishedFundingUndoTasks()
        {
            TheTaskIs<PublishedFundingUndoTask>(Factory.CreatePublishedFundingUndoTask(),
                (_ => _.IsHardDelete, nameof(PublishedFundingUndoTask.IsHardDelete)));
        }

        [TestMethod]
        public void CreatesHardDeletePublishedFundingVersionUndoTasks()
        {
            TheTaskIs<HardDeletePublishedFundingVersionUndoTask>(Factory.CreatePublishedFundingVersionUndoTask());
        }

        [TestMethod]
        public void CreateHardDeleteUndoTasksBasedOnContext()
        {
            PublishedFundingUndoTaskContext context = NewPublishedFundingUndoTaskContext(_ =>
            _.WithPublishedFundingDetails(NewUndoTaskDetails())
            .WithPublishedFundingVersionDetails(NewUndoTaskDetails())
            .WithPublishedProviderDetails(NewUndoTaskDetails())
            .WithPublishedProviderVersionDetails(NewUndoTaskDetails()));

            IEnumerable<IPublishedFundingUndoJobTask> undoTasks = WhenTheFactoryCreatesUndoTasks(context);

            undoTasks.Select(x => x.GetType())
                .Should()
                .BeEquivalentTo(new[]
                {
                    typeof(PublishedProviderUndoTask),
                    typeof(HardDeletePublishedProviderVersionsUndoTask),
                    typeof(PublishedFundingUndoTask),
                    typeof(HardDeletePublishedFundingVersionUndoTask)
                }, opt
                    => opt.WithoutStrictOrdering());
        }

        private IEnumerable<IPublishedFundingUndoJobTask> WhenTheFactoryCreatesUndoTasks(PublishedFundingUndoTaskContext context)
        {
            return Factory.CreateUndoTasks(context);
        }
    }
}