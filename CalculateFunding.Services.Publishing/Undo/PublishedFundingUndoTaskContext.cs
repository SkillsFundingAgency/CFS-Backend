using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CalculateFunding.Common.Utility;

namespace CalculateFunding.Services.Publishing.Undo
{
    public class PublishedFundingUndoTaskContext
    {
        public PublishedFundingUndoTaskContext(PublishedFundingUndoJobParameters parameters)
        {
            Guard.ArgumentNotNull(parameters, nameof(parameters));
            
            Parameters = parameters;
        }

        public PublishedFundingUndoJobParameters Parameters { get; }
        
        public CorrelationIdDetails PublishedProviderDetails { get; set; }
        
        public CorrelationIdDetails PublishedProviderVersionDetails { get; set; }
        
        public CorrelationIdDetails PublishedFundingDetails { get; set; }
        
        public CorrelationIdDetails PublishedFundingVersionDetails { get; set; }
        
        public ICollection<Exception> Errors { get; } = new List<Exception>();

        public volatile int CompletedTaskCount;

        public void IncrementCompletedTaskCount()
        {
            Interlocked.Increment(ref CompletedTaskCount);
        }

        public (bool isInitialised, IEnumerable<string> errors) EnsureIsInitialised()
        {
            List<string> errors = new List<string>();
            
            GuardAgainstNull(PublishedFundingDetails, nameof(PublishedFundingDetails), errors);
            GuardAgainstNull(PublishedFundingVersionDetails, nameof(PublishedFundingVersionDetails), errors);
            GuardAgainstNull(PublishedProviderDetails, nameof(PublishedProviderDetails), errors);
            GuardAgainstNull(PublishedFundingVersionDetails, nameof(PublishedFundingVersionDetails), errors);

            return (!errors.Any(), errors);
        }

        public void RegisterException(Exception exception)
        {
            Errors.Add(exception);
        }

        private void GuardAgainstNull(object property, string name, ICollection<string> errors)
        {
            if (property != null)
            {
                return;
            }
            
            errors.Add($"{name} missing in task context");
        }
    }
}