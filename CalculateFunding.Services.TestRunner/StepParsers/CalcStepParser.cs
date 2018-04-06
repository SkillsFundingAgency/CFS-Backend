using System.Collections.Generic;

namespace CalculateFunding.Services.TestRunner.StepParsers
{
    public abstract class CalcStepParser
    {
       protected static IEnumerable<string> ComparisonOperations = new[]
       {
            "is greater than",
            "is greater than or equal to",
            "is less than",
            "is less than or equal to",
            "is equal to",
            "is not equal to"
        };
    }
}
