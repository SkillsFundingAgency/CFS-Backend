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
    public class ConverterReProfileVariationChangeTests : MidYearReProfilingVariationChangeTests
    {
        protected override string Strategy => "ConverterReProfiling";

        protected override string ChangeName => "Converter re-profile variation change";

        protected override MidYearType MidYearTypeValue => MidYearType.Converter;

        [TestInitialize]
        public override void SetUp()
        {
            Change = new ConverterReProfileVariationChange(VariationContext, Strategy);
        }
    }
}