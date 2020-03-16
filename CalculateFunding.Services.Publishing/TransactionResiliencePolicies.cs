using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Publishing
{
    public class TransactionResiliencePolicies : ITransactionResiliencePolicies
    {
        public Policy TransactionPolicy { get; set; }
    }
}
