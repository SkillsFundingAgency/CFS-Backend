using System;

namespace CalculateFunding.Services.Core
{
    public class InvalidModelException : ApplicationException
    {
        public InvalidModelException(string modelType, string[] errors)
            : base($"The model for type: {modelType} is invalid with the following errors {string.Join(";", errors)}")
        {
        }
    }
}
