using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Models
{
    internal class ExpectedProviderVersionChannel
    {
        public int ReleasedProviderVersionChannelId { get; set; }

        public int ReleasedProviderVersionId { get; set; }

        public string Channel { get; set; }

        public DateTime StatusChangedDate { get; set; }

        public string AuthorId { get; set; }

        public string AuthorName { get; set; }
    }
}
