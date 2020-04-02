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
    public class Transaction : IDisposable
    {
        private readonly ILogger _logger;
        private readonly AsyncPolicy _transactionPolicy;
        private readonly ConcurrentBag<Func<Task>> _functions;
        private readonly string _transactionName;
        private bool _committed;

        public Transaction(ILogger logger, ITransactionResiliencePolicies transactionResiliencePolicies, string transactionName)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(transactionResiliencePolicies, nameof(transactionResiliencePolicies));
            Guard.ArgumentNotNull(transactionResiliencePolicies.TransactionPolicy, nameof(transactionResiliencePolicies.TransactionPolicy));
            Guard.IsNullOrWhiteSpace(transactionName, nameof(transactionName));

            _functions = new ConcurrentBag<Func<Task>>();
            _logger = logger;
            _transactionPolicy = transactionResiliencePolicies.TransactionPolicy;
            _transactionName = transactionName;
        }

        public void Enroll(Func<Task> function)
        {
            Guard.ArgumentNotNull(function, nameof(function));

            _functions.Add(function);
        }

        public void Complete()
        {
            try
            {
                _logger.Information($"{_transactionName} transaction completed");
            }
            finally
            {
                _committed = true;
            }
        }

        public async Task<bool> Compensate()
        {
            try
            {
                bool errored = false;

                while (!errored && _functions.TryTake(out Func<Task> function))
                {
                    try
                    {
                        await _transactionPolicy.ExecuteAsync(() => function());
                    }
                    catch(Exception ex)
                    {
                        _logger.Error(ex, $"{function.Method.Name} failed to complete successfully");
                        errored = true;
                    }
                }

                if (!errored)
                {
                    _logger.Information($"{_transactionName} compensated");
                }

                return errored;
            }
            finally
            {
                _committed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_committed)
            {
                Task.WaitAll(Compensate());
            }
        }
    }
}
