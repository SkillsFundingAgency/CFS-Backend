using System;

namespace CalculateFunding.Models.Publishing
{
    public class PublishedProviderCreateVersionRequest
    {
        public PublishedProvider PublishedProvider { get; set; }

        public PublishedProviderVersion NewVersion { get; set; }

        public override bool Equals(object obj)
        {
            return GetHashCode().Equals(obj?.GetHashCode());
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PublishedProvider, NewVersion);
        }
    }
}
