using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Repositories.Common.Search
{
    public class FailedToIndexSearchException : ApplicationException
    {
        public FailedToIndexSearchException(IEnumerable<IndexError> errors)
            : base($"failed to index search with the following errors: {string.Join(";", errors.Select(m => m.ErrorMessage))}")
        {

        }
    }
}
