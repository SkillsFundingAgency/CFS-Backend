using CalculateFunding.Services.Publishing.Interfaces.Undo;
using CalculateFunding.Services.Publishing.Undo;
using CalculateFunding.Services.Publishing.Undo.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Publishing.UnitTests.Undo
{
    [TestClass]
    public class SoftDeletePublishedFundingUndoTaskFactoryTests : PublishedFundingUndoTaskFactoryTest
    {
        [TestInitialize]
        public void SetUp()
        {
            Factory = new SoftDeletePublishedFundingUndoTaskFactory(Cosmos.Object,
                BlobStore.Object,
                ProducerConsumerFactory,
                Logger,
                JobTracker.Object);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void IsForSoftDeleteJobs(bool hardDelete)
        {
            GivenTheParametersIsHardDelete(hardDelete);

            bool isForJob = WhenIsForJobIsQueried();

            isForJob
                .Should()
                .Be(!hardDelete);
        }

        [TestMethod]
        public void CreatesSoftDeletePublishedProviderUndoTasks()
        {
            TheTaskIs<PublishedProviderUndoTask>(Factory.CreatePublishedProviderUndoTask(),
                (_ => _.IsHardDelete == false, nameof(PublishedProviderUndoTask.IsHardDelete)));    
        }
        
        [TestMethod]
        public void CreatesSoftDeletePublishedProviderVersionUndoTasks()
        {
            TheTaskIs<SoftDeletePublishedProviderVersionsUndoTask>(Factory.CreatePublishedProviderVersionUndoTask());    
        }
        
        [TestMethod]
        public void CreatesSoftDeletePublishedFundingUndoTasks()
        {
            TheTaskIs<PublishedFundingUndoTask>(Factory.CreatePublishedFundingUndoTask(),
                (_ => _.IsHardDelete == false, nameof(PublishedFundingUndoTask.IsHardDelete)));    
        }
        
        [TestMethod]
        public void CreatesSoftDeletePublishedFundingVersionUndoTasks()
        {
            TheTaskIs<SoftDeletePublishedFundingVersionUndoTask>(Factory.CreatePublishedFundingVersionUndoTask());    
        }

        [TestMethod]
        public void CreateSoftDeleteUndoTasksBasedOnContext()
        {
            PublishedFundingUndoTaskContext context = NewPublishedFundingUndoTaskContext(_ =>
            _.WithPublishedProviderVersionDetails(NewUndoTaskDetails()));

            IEnumerable<IPublishedFundingUndoJobTask> undoTasks = WhenTheFactoryCreatesUndoTasks(context);

            undoTasks.Select(x => x.GetType())
                .Should()
                .BeEquivalentTo(new[]
                {
                    typeof(PublishedProviderUndoTask),
                    typeof(SoftDeletePublishedProviderVersionsUndoTask),
                    typeof(PublishedFundingUndoTask),
                    typeof(SoftDeletePublishedFundingVersionUndoTask)
                }, opt
                    => opt.WithoutStrictOrdering());
        }

        private IEnumerable<IPublishedFundingUndoJobTask> WhenTheFactoryCreatesUndoTasks(PublishedFundingUndoTaskContext context)
        {
            return Factory.CreateUndoTasks(context);
        }
    }
}