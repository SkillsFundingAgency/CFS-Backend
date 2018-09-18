using System;

namespace CalculateFunding.Models.Exceptions
{
    public class MissingProviderProfilesException : ApplicationException
    {
        public MissingProviderProfilesException(string resultId, string providerId)
            : base($"Provider result with id {resultId} and provider id {providerId} contains no profiling periods")
        {
        }
    }
}
