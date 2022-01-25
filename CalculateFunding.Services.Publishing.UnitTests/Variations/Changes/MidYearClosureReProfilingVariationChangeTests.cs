using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations.Changes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    [TestClass]
    public class MidYearClosureReProfilingVariationChangeTests : MidYearReProfilingVariationChangeTests
    {
        protected override string Strategy => "MidYearClosureReProfiling";

        protected override MidYearType MidYearTypeValue => MidYearType.Closure;

        protected override PublishedProviderVersion ReProfilePublishedProvider => PriorState;

        [TestInitialize]
        public override void SetUp()
        {
            Change = new MidYearClosureReProfileVariationChange(VariationContext, Strategy);
        }
    }
}