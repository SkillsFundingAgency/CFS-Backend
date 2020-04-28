using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.Models
{
    public class PublishProvidersRequest
    {
        public IEnumerable<string> Providers { get; set; }
    }
}
