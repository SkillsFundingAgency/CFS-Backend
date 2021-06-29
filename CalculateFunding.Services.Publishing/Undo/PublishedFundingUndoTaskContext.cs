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
        
        public UndoTaskDetails UndoTaskDetails { get; set; }
        
        public ICollection<Exception> Errors { get; } = new List<Exception>();

        public volatile int CompletedTaskCount;

        public void IncrementCompletedTaskCount()
        {
            Interlocked.Increment(ref CompletedTaskCount);
        }

        public (bool isInitialised, IEnumerable<string> errors) EnsureIsInitialised()
        {
            List<string> errors = new List<string>();
            
            if (UndoTaskDetails == null)
            {
                errors.Add($"No funding or provider details in the task context.");
            }


            return (!errors.Any(), errors);
        }

        public void RegisterException(Exception exception)
        {
            Errors.Add(exception);
        }
    }
}