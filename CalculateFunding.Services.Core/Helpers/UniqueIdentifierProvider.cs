using CalculateFunding.Services.Core.Interfaces;
using System;

namespace CalculateFunding.Services.Core.Helpers
{
    public class UniqueIdentifierProvider : IUniqueIdentifierProvider
    {
        public string CreateUniqueIdentifier() => Guid.NewGuid().ToString();

        public Guid GenerateIdentifier() => Guid.NewGuid();
    }
}