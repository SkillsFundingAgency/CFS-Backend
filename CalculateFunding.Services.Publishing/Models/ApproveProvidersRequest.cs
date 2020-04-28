using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.Models
{
    public class ApproveProvidersRequest
    {
        public IEnumerable<string> Providers { get; set; }
    }
}
