using System;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Extensions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Models.UnitTests.Jobs
{
    [TestClass]
    public class JobTests
    {
        private Job _job;
        
        [TestInitialize]
        public void SetUp()
        {
            _job = new Job();
        }

        [TestMethod]
        public void DiscardsDuplicateOutcomesWhenAdded()
        {
            Outcome outcomeOne = NewOutcome();
            Outcome outcomeTwo = outcomeOne.DeepCopy();
            
            _job.AddOutcome(outcomeOne);
            _job.AddOutcome(outcomeTwo);

            _job.Outcomes
                .Should()
                .BeEquivalentTo(new object[]
                {
                    outcomeOne
                });
        }

        private Outcome NewOutcome(Action<OutcomeBuilder> setUp = null)
        {
            OutcomeBuilder outcomeBuilder = new OutcomeBuilder();

            setUp?.Invoke(outcomeBuilder);
            
            return outcomeBuilder.Build();
        }
    }
}