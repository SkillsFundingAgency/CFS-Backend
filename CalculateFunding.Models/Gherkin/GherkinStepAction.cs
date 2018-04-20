using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;

namespace CalculateFunding.Models.Gherkin
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

        protected static object GetActualValue(IEnumerable<ProviderSourceDataset> datasets, string datasetRelationshipName, string fieldName)
        {
            object actualValue = null;

            //var datasetsByType = new Dictionary<string, ProviderSourceDataset>();

            //foreach (var dataset in datasets)
            //{
            //    var field = dataset.GetType().GetProperty("DataRelationship");
            //    var relationship = field.GetValue(dataset) as Reference;
            //    datasetsByType.Add(relationship.Name, dataset);
            //}

            ProviderSourceDataset providerSourceDataset = datasets.Where(d => d.DataRelationship.Name == datasetRelationshipName).FirstOrDefault();
            if (providerSourceDataset != null)
            {
                var rows = providerSourceDataset.Current.Rows;

                if (rows.Count > 0)
                {
                    actualValue = rows.First()[fieldName];
                }
            }

            return actualValue;
        }
    }
}