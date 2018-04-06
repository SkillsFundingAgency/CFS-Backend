using System;
using System.Collections.Generic;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;

namespace CalculateFunding.Services.TestRunner
{

    public interface IStepAction
    {
        GherkinParseResult Execute(ProviderResult providerResult, IEnumerable<ProviderSourceDataset> datasets);
    }

    public abstract class GherkinStepAction : IStepAction
    {
        public abstract GherkinParseResult Execute(ProviderResult providerResult, IEnumerable<ProviderSourceDataset> datasets);
        protected bool TestLogic(object expectedValue, object actualValue, ComparisonOperator logic)
        {
            var expected = expectedValue as IComparable;
            var actual = actualValue as IComparable;

            if (expected != null && actual == null) return false;

            switch (logic)
            {
                case ComparisonOperator.EqualTo:
                    return actual.CompareTo(expected)  == 0;
                case ComparisonOperator.NotEqualTo:
                    return actual.CompareTo(expected) != 0;
                case ComparisonOperator.GreaterThan:                 
                    return actual.CompareTo(expectedValue) > 0;
                case ComparisonOperator.GreaterThanOrEqualTo:
                    return actual.CompareTo(expectedValue) >= 0;
                case ComparisonOperator.LessThan:
                    return actual.CompareTo(expectedValue) < 0;
                case ComparisonOperator.LessThanOrEqualTo:
                    return actual.CompareTo(expectedValue) <= 0;
                default: return false;
            }
        }

        protected static object GetActualValue(IEnumerable<ProviderSourceDataset> datasets, string datasetName, string fieldName)
        {
            object actualValue = null;


            var datasetsByType = new Dictionary<string, object>();
            foreach (var dataset in datasets)
            {
                var field = dataset.GetType().GetField("DatasetDefinitionName");
                var definitionName = field.GetValue(null).ToString();
                datasetsByType.Add(definitionName, dataset);
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