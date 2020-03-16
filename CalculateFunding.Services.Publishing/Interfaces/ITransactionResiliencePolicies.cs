using Polly;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface ITransactionResiliencePolicies
    {
        Policy TransactionPolicy { get; set; }
    }
}
