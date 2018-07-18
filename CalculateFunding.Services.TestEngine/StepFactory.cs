using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Gherkin;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Specs;
using Gherkin.Ast;

namespace CalculateFunding.Services.TestRunner
{
    public class StepFactory
    {
        public StepFactory(params GherkinStepAction[] stepsActions)
        {
            StepsAction = stepsActions.Select(x => x.GetType()).Where(x => x.GetCustomAttribute<TestStepAttribute>() != null).ToList();
        }

        public IEnumerable<GherkinError> Validate(Step step, SpecificationCurrentVersion specification,
            List<DefinitionSpecificationRelationship> dataRelationships,
            List<DatasetDefinition> dataDefinitions)
        {
            foreach (var stepAction in StepsAction)
            {
                var stepDefinition = stepAction.GetCustomAttribute<TestStepAttribute>();
                if (stepDefinition.Keyword == step.Keyword?.Trim().ToLowerInvariant())
                {
                    if (Regex.IsMatch(step.Text, stepDefinition.Regex))
                    {
                        var arguments = Regex.Split(step.Text, stepDefinition.Regex).Skip(1).ToArray();
                        var instance = Activator.CreateInstance(stepAction) as GherkinStepAction;
                        var propertyInfos = stepAction.GetProperties()
                            .Where(x => x.GetCustomAttribute<TestStepArgumentAttribute>() != null).ToArray();

                        string datasetName = null;
                        string fieldName = null;

                        for (var index = 0; index < propertyInfos.Length; index++)
                        {
                            var propertyInfo = propertyInfos[index];
                            var argument = arguments[index];
                            var argumentDefinition = propertyInfo.GetCustomAttribute<TestStepArgumentAttribute>();
                            switch (argumentDefinition.Type)
                            {
                                case StepArgumentType.DatasetName:
                                    datasetName = argument;
                                    if (!ValidateField(argument, fieldName, dataRelationships, dataDefinitions))
                                    {
                                        yield return new GherkinError($"'{argument} is not a valid dataset name'", step.Location.Line, step.Location.Column);
                                        propertyInfo.SetValue(instance, argument);
                                    }
                                    break;
                                case StepArgumentType.FieldName:
                                    fieldName = argument;
                                    if (!ValidateField(datasetName, argument, dataRelationships, dataDefinitions))
                                    {
                                        yield return new GherkinError($"'{argument} is not a valid field name in {datasetName}'", step.Location.Line, step.Location.Column);
                                        propertyInfo.SetValue(instance, argument);
                                    }
                                    break;
                                case StepArgumentType.CalculationName:
                                    if (!ValidateCalculation(argument, specification))
                                    {
                                        yield return new GherkinError($"'{argument} is not a valid calculation name'", step.Location.Line, step.Location.Column);
                                        propertyInfo.SetValue(instance, argument);
                                    }

                                    break;
                            }
                        }
                    }
                }
            }
        }


        private bool ValidateField(string datasetName, string fieldName, List<DefinitionSpecificationRelationship> dataRelationships, List<DatasetDefinition> datasetDefinitions)
        {
            var datasets = datasetName != null ? dataRelationships.Where(x =>  x.Name == datasetName).Select(x => x.DatasetDefinition.Id).ToList() : null;

            if (fieldName != null)
            {
                var definitions = datasets != null ? datasetDefinitions.Where(x => datasets.Contains(x.Id)) : datasetDefinitions;
                return definitions.SelectMany(x => x.TableDefinitions?.FirstOrDefault()?.FieldDefinitions).Any(x => x.Name == fieldName);
            }

            return datasets.Any();
        }

        private bool ValidateCalculation(string argument, SpecificationCurrentVersion specification)
        {
            return specification.GetCalculations().SingleOrDefault(x => x.Name == argument) != null;
        }


        public GherkinStepAction GetAction(Step step)
        {
            foreach (var stepAction in StepsAction)
            {
                var stepDefinition = stepAction.GetCustomAttribute<TestStepAttribute>();
                if (stepDefinition.Keyword == step.Keyword?.Trim().ToLowerInvariant())
                {
                    if (Regex.IsMatch(step.Text, stepDefinition.Regex))
                    {
                        var instance = Activator.CreateInstance(stepAction) as GherkinStepAction;
                        foreach (var propertyInfo in stepAction.GetProperties().Where(x => x.GetCustomAttribute<TestStepArgumentAttribute>() != null))
                        {
                            
                        }
                        return instance;
                    }
                }

            }
            return null;
        }

        protected List<Type> StepsAction { get; }
    }
}