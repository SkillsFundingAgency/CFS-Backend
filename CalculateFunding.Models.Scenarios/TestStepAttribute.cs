using System;

namespace CalculateFunding.Models.Scenarios
{
    public class TestStepAttribute : Attribute
    {
        public string Keyword { get; }
        public string Regex { get; }

        public TestStepAttribute(string keyword, string regex)
        {
            Keyword = keyword;
            Regex = regex;
        }
    }
}