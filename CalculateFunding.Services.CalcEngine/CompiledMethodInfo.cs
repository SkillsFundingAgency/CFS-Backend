using System;
using System.Linq.Expressions;
using System.Reflection;

namespace CalculateFunding.Services.CalcEngine
{
    public class CompiledMethodInfo
    {
        private delegate decimal? ReturnValueDelegate(object instance);

        public CompiledMethodInfo(MethodInfo methodInfo)
        {
            ParameterExpression instanceExpression = Expression.Parameter(typeof(object), "instance");

            MethodCallExpression callExpression = Expression.Call(Expression.Convert(instanceExpression, methodInfo.ReflectedType), methodInfo);

            Delegate = Expression.Lambda<ReturnValueDelegate>(Expression.Convert(callExpression, typeof(decimal?)), instanceExpression).Compile();
        }

        private ReturnValueDelegate Delegate { get; set; }

        public decimal? Execute(object instance)
        {
            return Delegate(instance);
        }
    }
}
