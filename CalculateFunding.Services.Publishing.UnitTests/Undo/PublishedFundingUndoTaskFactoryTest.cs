using System;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using CalculateFunding.Services.Publishing.Undo;
using CalculateFunding.Services.Publishing.Undo.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Undo
{
    public abstract class PublishedFundingUndoTaskFactoryTest : UndoTaskTestBase
    {
        protected IPublishedFundingUndoTaskFactory Factory;

        [TestMethod]
        public void CreateContextInitialisationTasks()
        {
            TheTaskIs<PublishedFundingUndoContextInitialisationTask>(Factory.CreateContextInitialisationTask());    
        }

        protected void GivenTheParametersIsHardDelete(bool isHardDelete)
        {
            Parameters = NewPublishedFundingUndoJobParameters(_ => _.WithIsHardDelete(isHardDelete));
        }

        protected void TheTaskIs<TTask>(IPublishedFundingUndoJobTask task, 
            (Func<TTask, bool> checks, string message) extraChecks = default)
            where TTask : UndoTaskBase, IPublishedFundingUndoJobTask
        {
            task
                .Should()
                .BeOfType<TTask>();

            TTask concreteTask = (TTask) task;
            
            concreteTask.Cosmos
                .Should()
                .BeSameAs(Cosmos.Object);

            concreteTask.BlobStore
                .Should()
                .BeSameAs(BlobStore.Object);

            concreteTask.Logger
                .Should()
                .BeSameAs(Logger);

            concreteTask.JobTracker
                .Should()
                .BeSameAs(JobTracker.Object);

            if (extraChecks == default)
            {
                return;
            }

            extraChecks.checks(concreteTask)
                .Should()
                .BeTrue(extraChecks.message);
        }

        protected bool WhenIsForJobIsQueried()
        {
            return Factory.IsForJob(Parameters);
        }
    }
}