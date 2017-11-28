using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CalculateFunding.Models;

namespace CalculateFunding.Services.Calculator
{
    public class AllocationModel
    {
        public List<object> AllocationProcessors  = new List<object>();


        public IEnumerable<CalculationResult> Execute(string modelName, object[] datasets)
        {
            foreach (var allocation in AllocationProcessors)
            {
                var allocationType = allocation.GetType();
                var setters = allocationType.GetProperties().Where(x => x.CanWrite).ToArray();

                foreach (var dataset in datasets)
                {
                    foreach (var setter in setters.Where(x => x.PropertyType == dataset.GetType()))
                    {
                        setter.SetValue(allocation, dataset);
                    }
                }

                var executeMethods = allocationType.GetMethods().Where(x => x.ReturnType == typeof(decimal));
                foreach (var executeMethod in executeMethods)
                {
                    decimal result = decimal.Zero;

                    ParameterInfo[] parameters = executeMethod.GetParameters();

                    Exception exception= null;
                    if (parameters.Length == 0)
                    {
                        try
                        {
                            result = (decimal) executeMethod.Invoke(allocation, null);
                        }
                        catch (Exception e)
                        {
                            exception = e;
                        }

                    }

                    if (exception != null)
                    {
                      //  yield return new CalculationResult(exception);
                    }
                    else
                    {
                        yield return new CalculationResult(executeMethod.Name, result);
                    }



                }


            }

        }


    }
}