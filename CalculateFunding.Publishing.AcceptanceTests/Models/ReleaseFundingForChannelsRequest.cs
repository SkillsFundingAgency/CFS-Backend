using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Models
{
    internal class ReleaseFundingForChannelsRequest
    {
        public string CorrelationId { get; set; }
        public string AuthorName { get; set; }
        public string AuthorId { get; set; }
    }
}
