using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Allocations.Models;
using Allocations.Models.Results;
using Allocations.Models.Specs;
using Gherkin.Ast;

namespace Allocations.Services.TestRunner
{
    public abstract class GherkinStepAction
    {
        protected GherkinStepAction(string regularExpression, params string[] keywords)
        {
            Keywords = keywords;
            RegularExpression = new Regex(regularExpression);
        }

        public Regex RegularExpression { get; }

        public string[] Keywords { get; }

        protected IEnumerable<string> GetInlineArguments(Step step)
        {
            var group = RegularExpression.Match(step.Text).Groups;
            for (var i = 1; i < group.Count; i++)
            {
                yield return group[i].Value;
            }
        }

        public abstract GherkinResult Validate(Budget budget, Step step);

        public abstract GherkinResult Execute(ProductResult productResult, List<object> datasets, Step step);

        protected bool TestLogic(object expectedValue, object actualValue, string logic)
        {
            var expected = expectedValue as IComparable;
            var actual = actualValue as IComparable;

            switch (logic.Trim().ToLowerInvariant())
            {
                case "equal to":
                    return actual.CompareTo(expected)  == 0;
                case "not equal to":
                    return actual.CompareTo(expected) != 0;
                case "greater than":                 
                    return actual.CompareTo(expectedValue) > 0;
                case "greater than or equal to":
                    return actual.CompareTo(expectedValue) >= 0;
                case "less than":
                    return actual.CompareTo(expectedValue) < 0;
                case "less than or equal to":
                    return actual.CompareTo(expectedValue) <= 0;
                default: return false;
            }
        }

        protected static object GetActualValue(List<object> datasets, string datasetName, string fieldName)
        {
            object actualValue = null;


            var datasetsByType = new Dictionary<string, object>();
            foreach (var dataset in datasets)
            {
                datasetsByType.Add(dataset.GetType().GetCustomAttribute<DatasetAttribute>().DatasetName, dataset);
            }

            if (datasetsByType.TryGetValue(datasetName, out var selectedDataset))
            {
                foreach (var prop in selectedDataset.GetType().GetProperties())
                {
                    if (prop.Name == fieldName)
                    {
                        actualValue = prop.GetValue(selectedDataset);
                    }
                }
            }
            return actualValue;
        }

    }
}