using CalculateFunding.Models.Scenarios;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace CalculateFunding.Services.TestRunner.StepParsers
{
    public abstract class CalcStepParser
    {
       //protected static IEnumerable<string> ComparisonOperations = new[]
       //{
       //     "is greater than",
       //     "is greater than or equal to",
       //     "is less than",
       //     "is less than or equal to",
       //     "is equal to",
       //     "is not equal to"
       // };

        protected static IDictionary<ComparisonOperator, string> ComparisonOperators = new Dictionary<ComparisonOperator, string>
        {
            { ComparisonOperator.GreaterThan, "is greater than" },
            { ComparisonOperator.GreaterThanOrEqualTo, "is greater than or equal to" },
            { ComparisonOperator.LessThan, "is less than" },
            { ComparisonOperator.LessThanOrEqualTo, "is less than or equal to" },
            { ComparisonOperator.EqualTo, "is equal to" },
            { ComparisonOperator.NotEqualTo, "is not equal to" },
        };
    }
}
