using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using CalculateFunding.Services.Publishing.Undo;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog.Core;

namespace CalculateFunding.Services.Publishing.UnitTests.Undo
{
    [TestClass]
    public class PublishedFundingUndoJobServiceTests : PublishedFundingUndoTestBase
    {
        private Mock<IPublishedFundingUndoJobCreation> _jobCreation;
        private Mock<IPublishedFundingUndoTaskFactoryLocator> _taskFactoryLocator;
        private Mock<IPublishedFundingUndoTaskFactory> _taskFactory;
        private Mock<IJobManagement> _jobManagement;
        private Mock<IPublishedFundingUndoJobTask> _initialiseTask;
        private Mock<IPublishedFundingUndoJobTask> _undoPublishedProvidersTask;
        private Mock<IPublishedFundingUndoJobTask> _undoPublishedProviderVersionsTask;
        private Mock<IPublishedFundingUndoJobTask> _undoPublishedFundingTask;
        private Mock<IPublishedFundingUndoJobTask> _undoPublishedFundingVersionsTask;

        private Message _message;
        
        private IPublishedFundingUndoJobService _service;

        private List<Mock<IPublishedFundingUndoJobTask>> _invokedTasks;
        
        [TestInitialize]
        public void SetUp()
        {
            _jobCreation = new Mock<IPublishedFundingUndoJobCreation>();
            _taskFactoryLocator = new Mock<IPublishedFundingUndoTaskFactoryLocator>();
            _taskFactory = new Mock<IPublishedFundingUndoTaskFactory>();
            _jobManagement = new Mock<IJobManagement>();

            _initialiseTask = NewMockTask();
            _undoPublishedFundingTask = NewMockTask();
            _undoPublishedFundingVersionsTask = NewMockTask();
            _undoPublishedProvidersTask = NewMockTask();
            _undoPublishedProviderVersionsTask = NewMockTask();

            _taskFactory.Setup(_ => _.CreateContextInitialisationTask())
                .Returns(_initialiseTask.Object);
            _taskFactory.Setup(_ => _.CreatePublishedFundingUndoTask())
                .Returns(_undoPublishedFundingTask.Object);
            _taskFactory.Setup(_ => _.CreatePublishedFundingVersionUndoTask())
                .Returns(_undoPublishedFundingVersionsTask.Object);
            _taskFactory.Setup(_ => _.CreatePublishedProviderUndoTask())
                .Returns(_undoPublishedProvidersTask.Object);
            _taskFactory.Setup(_ => _.CreatePublishedProviderVersionUndoTask())
                .Returns(_undoPublishedProviderVersionsTask.Object);
            
            _service = new PublishedFundingUndoJobService(_taskFactoryLocator.Object,
                _jobManagement.Object,
                _jobCreation.Object,
                Logger.None);
            
            _message = new Message();
            
            _invokedTasks = new List<Mock<IPublishedFundingUndoJobTask>>();
        }

        [TestMethod]
        public void GuardsAgainstMissingForCorrelationIdInMessage()
        {
            GivenTheMessageProperties(("is-hard-delete", NewRandomFlag().ToString()), 
                ("jobId", NewRandomString()));

            Func<Task> invocation = WhenTheJobIsRun;

            invocation
                .Should()
                .Throw<ArgumentOutOfRangeException>()
                .Which
                .ParamName
                .Should()
                .Be("for-correlation-id");
        }
        
        [TestMethod]
        public void GuardsAgainstMissingJobIdInMessage()
        {
            GivenTheMessageProperties(("is-hard-delete", NewRandomFlag().ToString()), 
                ("for-correlation-id", NewRandomString()));

            Func<Task> invocation = WhenTheJobIsRun;

            invocation
                .Should()
                .Throw<ArgumentOutOfRangeException>()
                .Which
                .ParamName
                .Should()
                .Be("jobId");
        }
        
        [TestMethod]
        public void GuardsAgainstMissingIsHardDeleteInMessage()
        {
            GivenTheMessageProperties(("jobId", NewRandomString()), 
                ("for-correlation-id", NewRandomString()));

            Func<Task> invocation = WhenTheJobIsRun;

            invocation
                .Should()
                .Throw<ArgumentOutOfRangeException>()
                .Which
                .ParamName
                .Should()
                .Be("is-hard-delete");
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task BuildsTasksForSuppliedParametersAndRunsJob(bool isHardDelete)
        {
            string jobId = NewRandomString();
            
            GivenTheMessageProperties(("jobId", jobId),
                ("for-correlation-id", NewRandomString()),
                ("is-hard-delete", isHardDelete.ToString()));
            AndTheTaskFactoryIsForTheSuppliedParameters();
            AndTheJobCanBeTracked(jobId);
            
            await WhenTheJobIsRun();
            
            ThenTheContextWasInitialisedFirst();
            AndTheRemainingUndoTasksWereRunAfter();
            AndTheJobWasCompleted(jobId);
        }

        [TestMethod]
        public void ThrowsExceptionIfTaskContextCollectsExceptionsWhenTasksRun()
        {
            string jobId = NewRandomString();
            string correlationId = NewRandomString();

            GivenTheMessageProperties(("jobId", jobId),
                ("for-correlation-id", correlationId),
                ("is-hard-delete", NewRandomFlag().ToString()));
            AndTheTaskFactoryIsForTheSuppliedParameters();
            AndTheJobCanBeTracked(jobId);
            
            Func<Task> invocation = WhenTheJobIsRun;
            
            Exception expectedInnerException = new Exception();
            
            AndTheTaskCollectsException(_undoPublishedFundingVersionsTask, expectedInnerException);

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .Which
                .InnerException
                .Should()
                .BeOfType<InvalidOperationException>()
                .Which
                .InnerException
                .Should()
                .BeOfType<AggregateException>()
                .Which
                .InnerExceptions
                .Should()
                .BeEquivalentTo(new object[] { expectedInnerException });
            
            AndTheJobWasFailed(jobId, $"Unable to complete {JobConstants.DefinitionNames.PublishedFundingUndoJob} for correlationId: {correlationId}.\nUndo tasks generated unhandled exceptions");
        }

        [TestMethod]
        public void QueuesUndoJobsGuardsAgainstMissingForCorrelationId()
        {
            Func<Task> invocation = () => WhenTheJobIsQueued(null,
                NewRandomFlag(),
                NewUser(),
                NewRandomString());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("forCorrelationId");
        }
        
        [TestMethod]
        public void QueuesUndoJobsGuardsAgainstMissingUser()
        {
            Func<Task> invocation = () => WhenTheJobIsQueued(NewRandomString(),
                NewRandomFlag(),
                null,
                NewRandomString());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("user");
        }
        
        private async Task<Job> WhenTheJobIsQueued(string forCorrelationId,
            bool isHardDelete,
            Reference user,
            string correlationId)
        {
            return await _service.QueueJob(forCorrelationId,
                isHardDelete,
                user,
                correlationId);
        }
        
        private async Task WhenTheJobIsRun()
        {
            await _service.Run(_message);
        }

        public void AndTheJobWasCompleted(string jobId)
        {
            _jobManagement.Verify(_ => _.UpdateJobStatus(jobId, 0, 0, true, null),
                Times.Once);
        }

        public void AndTheJobWasFailed(string jobId, string outcome = null)
        {
            _jobManagement.Verify(_ => _.UpdateJobStatus(jobId, 0, 0, false, outcome),
                Times.Once);
        }

        public void ThenTheContextWasInitialisedFirst()
        {
            _invokedTasks.First()
                .Should()
                .BeSameAs(_initialiseTask);
        }

        public void AndTheRemainingUndoTasksWereRunAfter()
        {
            _invokedTasks
                .Should()
                .BeEquivalentTo(new[]
                {
                    _initialiseTask,
                    _undoPublishedFundingTask,
                    _undoPublishedProvidersTask,
                    _undoPublishedFundingVersionsTask,
                    _undoPublishedProviderVersionsTask
                }, opt
                    => opt.WithoutStrictOrdering());
        }

        private void AndTheJobCanBeTracked(string jobId)
        {
            _jobManagement.Setup(_ => _.UpdateJobStatus(jobId, 0, 0, null, null));
        }

        private void AndTheTaskCollectsException(Mock<IPublishedFundingUndoJobTask> task, Exception exception)
        {
            task.Setup(_ => _.Run(It.Is<PublishedFundingUndoTaskContext>(ctx =>
                    ctx.Parameters.ToString() == GetParametersString())))
                .Callback<PublishedFundingUndoTaskContext>(context => context.RegisterException(exception))
                .Returns(Task.CompletedTask);
        }

        private Mock<IPublishedFundingUndoJobTask> NewMockTask()
        {
            Mock<IPublishedFundingUndoJobTask> task = new Mock<IPublishedFundingUndoJobTask>();

            task.Setup(_ => _.Run(It.Is<PublishedFundingUndoTaskContext>(ctx =>
                    ctx.Parameters.ToString() == GetParametersString())))
                .Callback(() => _invokedTasks.Add(task))
                .Returns(Task.CompletedTask);
            
            return task;
        }

        private void GivenTheMessageProperties(params (string key, string value)[] properties)
        {
            _message.AddUserProperties(properties);
        }

        private void AndTheTaskFactoryIsForTheSuppliedParameters()
        {
            string parameters = GetParametersString();

            _taskFactoryLocator.Setup(_ => _.GetTaskFactoryFor(It.Is<PublishedFundingUndoJobParameters>(prm 
                    => prm.ToString() == parameters)))
                .Returns(_taskFactory.Object);
        }

        private string GetParametersString() => new PublishedFundingUndoJobParameters(_message).ToString();
        
        private Reference NewUser() => new ReferenceBuilder().Build();
    }
}