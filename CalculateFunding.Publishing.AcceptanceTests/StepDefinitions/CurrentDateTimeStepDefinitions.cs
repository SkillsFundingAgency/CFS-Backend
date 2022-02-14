using CalculateFunding.Services.Core.Services;
using CalculateFunding.Tests.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace CalculateFunding.Publishing.AcceptanceTests.StepDefinitions
{
    public class CurrentDateTimeStepDefinitions : StepDefinitionBase
    {
        private readonly StaticDateTimeService _svc;

        public CurrentDateTimeStepDefinitions(StaticDateTimeService staticDateTimeService)
        {
            _svc = staticDateTimeService;
        }

        [Given(@"the current date and time is '([^']*)'")]
        public void GivenTheCurrentDateAndTimeIs(DateTime   dateTime)
        {
            _svc.SetNow(dateTime);
        }

    }
}
