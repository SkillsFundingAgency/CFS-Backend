using System;
using CalculateFunding.Services.Core.Interfaces;

namespace CalculateFunding.Services.Core.Helpers
{
    public class UniqueIdentifierProvider : IUniqueIdentifierProvider
    {
        public string CreateUniqueIdentifier() => Guid.NewGuid().ToString();
    }
}