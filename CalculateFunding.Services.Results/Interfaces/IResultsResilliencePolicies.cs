using Polly;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IResultsResilliencePolicies
    {
        Policy CalculationProviderResultsSearchRepository { get; set; }

        Policy ResultsRepository { get; set; }

        Policy ResultsSearchRepository { get; set; }

        Policy SpecificationsRepository { get; set; }

        Policy AllocationNotificationFeedSearchRepository { get; set; }

        Policy ProviderProfilingRepository { get; set; }

        Policy PublishedProviderCalculationResultsRepository { get; set; }

        Policy PublishedProviderResultsRepository { get; set; }
    }
}
