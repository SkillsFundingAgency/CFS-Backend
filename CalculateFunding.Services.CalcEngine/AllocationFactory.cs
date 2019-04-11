using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Core.Helpers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CalculateFunding.Services.Calculator
{
    public class AllocationFactory : IAllocationFactory
    {
        private readonly ILogger _logger;
        private readonly IFeatureToggle _featureToggle;

        public AllocationFactory(ILogger logger, IFeatureToggle featureToggle)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));

            _logger = logger;
            _featureToggle = featureToggle;
        }

        public IAllocationModel CreateAllocationModel(Assembly assembly)
        {
            IEnumerable<Type> types = Enumerable.Empty<Type>();

            if (_featureToggle.IsUseFieldDefinitionIdsInSourceDatasetsEnabled())
            {
                types = assembly.GetTypes().Where(x => x.GetFields().Any(p => p.IsStatic && p.Name == "DatasetDefinitionId"));
            }
            else
            {
                types = assembly.GetTypes().Where(x => x.GetFields().Any(p => p.IsStatic && p.Name == "DatasetDefinitionName"));
            }

            Dictionary<string, Type> datasetTypes = new Dictionary<string, Type>();

            foreach (var type in types)
            {
                FieldInfo field = type.GetField(_featureToggle.IsUseFieldDefinitionIdsInSourceDatasetsEnabled() ? "DatasetDefinitionId" : "DatasetDefinitionName");
                string definitionName = field.GetValue(null).ToString();
                datasetTypes.Add(definitionName, type);
            }

            Type allocationType = assembly.GetTypes().FirstOrDefault(x => x.IsClass && x.BaseType.Name == "BaseCalculation");

            return new AllocationModel(allocationType, datasetTypes, _logger, _featureToggle);
        }

    }
}
