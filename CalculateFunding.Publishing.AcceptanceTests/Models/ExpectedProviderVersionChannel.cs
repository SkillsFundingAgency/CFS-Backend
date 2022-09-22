using System;

namespace CalculateFunding.Publishing.AcceptanceTests.Models
{
    internal class ExpectedProviderVersionChannel
    {
        public Guid ReleasedProviderVersionChannelId { get; set; }

        public Guid ReleasedProviderVersionId { get; set; }

        public string Channel { get; set; }

        public DateTime StatusChangedDate { get; set; }

        public string AuthorId { get; set; }

        public string AuthorName { get; set; }

        public int ChannelVersion { get; set; }
    }
}
