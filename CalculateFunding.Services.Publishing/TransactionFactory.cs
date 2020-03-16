using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing
{
    public class TransactionFactory : ITransactionFactory
    {
        private readonly ILogger _logger;
        private readonly ITransactionResiliencePolicies _transactionResiliencePolicies;

        public TransactionFactory(ILogger logger, ITransactionResiliencePolicies transactionResiliencePolicies)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(transactionResiliencePolicies, nameof(transactionResiliencePolicies));

            _logger = logger;
            _transactionResiliencePolicies = transactionResiliencePolicies;
        }

        public Transaction NewTransaction<T>() where T:class
        {
            return new Transaction(_logger, _transactionResiliencePolicies, typeof(T).Name);
        }
    }
}
