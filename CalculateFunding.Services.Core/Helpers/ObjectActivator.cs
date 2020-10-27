using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;

namespace CalculateFunding.Services.Core.Helpers
{
    public delegate object ObjectActivator(params object[] args);

    public class Activator
    {
        private readonly ConcurrentDictionary<string, ObjectActivator> Store = new ConcurrentDictionary<string, ObjectActivator>();

        public ObjectActivator GetActivator (ConstructorInfo ctor, string fullName)
        {
            if (!Store.TryGetValue(fullName, out ObjectActivator objectActivator))
            {
                ParameterInfo[] paramsInfo = ctor.GetParameters();

                //create a single param of type object[]
                ParameterExpression param =
                    Expression.Parameter(typeof(object[]), "args");

                Expression[] argsExp =
                    new Expression[paramsInfo.Length];

                //pick each arg from the params array 
                //and create a typed expression of them
                for (int i = 0; i < paramsInfo.Length; i++)
                {
                    Expression index = Expression.Constant(i);
                    Type paramType = paramsInfo[i].ParameterType;

                    Expression paramAccessorExp =
                        Expression.ArrayIndex(param, index);

                    Expression paramCastExp =
                        Expression.Convert(paramAccessorExp, paramType);

                    argsExp[i] = paramCastExp;
                }

                //make a NewExpression that calls the
                //ctor with the args we just created
                NewExpression newExp = Expression.New(ctor, argsExp);

                //create a lambda with the New
                //Expression as body and our param object[] as arg
                LambdaExpression lambda =
                    Expression.Lambda(typeof(ObjectActivator), newExp, param);

                objectActivator = (ObjectActivator)lambda.Compile();

                Store.TryAdd(fullName, objectActivator);
            }

            //compile it
            return objectActivator;
        }
    }
}
