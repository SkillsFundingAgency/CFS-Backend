using System;

namespace CalculateFunding.Services.Core.AspNet.OperationFilters
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class JsonBodyContentsAttribute : Attribute
    {
    }
}
