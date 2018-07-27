using System;
using System.Collections.Generic;
using CalculateFunding.Models.External;
using Swashbuckle.AspNetCore.Examples;

namespace CalculateFunding.Api.External.V1.Models.Examples
{
    public class PeriodExamples : IExamplesProvider
    {
        public object GetExamples()
        {
            return new List<Period>
            {
                new Period { PeriodType = "AY", PeriodId = "AY1718", StartDate = new DateTime(2017,9,1), EndDate = new DateTime(2018,8,30)},
                new Period { PeriodType = "AY", PeriodId = "AY1819", StartDate = new DateTime(2018,9,1), EndDate = new DateTime(2019,8,30)},
            };
        }
    }
}
