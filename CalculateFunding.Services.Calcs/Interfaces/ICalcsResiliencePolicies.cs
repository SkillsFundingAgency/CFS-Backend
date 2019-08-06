﻿using Polly;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ICalcsResiliencePolicies
    {
        Policy CalculationsRepository { get; set; }

        Policy CalculationsSearchRepository { get; set; }

        Policy CacheProviderPolicy { get; set; }

        Policy CalculationsVersionsRepositoryPolicy { get; set; }

        Policy BuildProjectRepositoryPolicy { get; set; }

        Policy SpecificationsRepositoryPolicy { get; set; }

        Policy MessagePolicy { get; set; }

        Policy JobsApiClient { get; set; }

        Policy SourceFilesRepository { get; set; }

        Policy DatasetsRepository { get; set; }

        Policy PoliciesApiClient { get; set; }
        Policy SpecificationsApiClient { get; set; }
    }
}
