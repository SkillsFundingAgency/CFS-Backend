using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CalculateFunding.Services.Calculator
{
    public class AllocationFactory
    {

        private AllocationModel AllocationModel { get; }
        
        public AllocationFactory(Assembly assembly)
        {

            var types = assembly.GetTypes().Where(x => x.GetFields().Any(p => p.IsStatic && p.Name == "DatasetDefinitionName"));
           var datasetTypes = new Dictionary<string, Type>();
            foreach (var type in types)
            {
                var field = type.GetField("DatasetDefinitionName");
                var definitionName = field.GetValue(null).ToString();
                datasetTypes.Add(definitionName, type);
            }

            var allocationType = assembly.GetTypes().FirstOrDefault(x => x.IsClass && x.BaseType.Name == "BaseCalculation");
            AllocationModel = new AllocationModel(allocationType, datasetTypes);
        }





        public AllocationModel CreateAllocationModel()
        {
            return AllocationModel;
        }


    }
}
