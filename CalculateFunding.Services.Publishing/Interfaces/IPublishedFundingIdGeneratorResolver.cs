using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedFundingIdGeneratorResolver
    {
        IPublishedFundingIdGenerator GetService(string schemaVersion);

        bool TryGetService(string schemaVersion, out IPublishedFundingIdGenerator publishedFundingContentsGenerator);

        bool Contains(string schemaVersion);

        void Register(string schemaVersion, IPublishedFundingIdGenerator publishedFundingContentsGenerator);
    }
}
