using Polly;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Calculator.Interfaces
{
    public interface ICalculatorResiliencePolicies
    {
        Policy CacheProvider { get; set; }

        Policy Messenger { get; set; }

        Policy ProviderSourceDatasetsRepository { get; set; }

        Policy ProviderResultsRepository { get; set; }

    }
}
